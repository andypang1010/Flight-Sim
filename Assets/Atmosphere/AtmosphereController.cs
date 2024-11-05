using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtmosphereController : MonoBehaviour
{
    private const float DAYLENGTH_SECONDS = 86400f;

    private const float STARS_DISAPPEAR_TIME_SECONDS = 3600;
    private const float SUNRISE_TIME_SECONDS = 21600f;
    private const float NOON_TIME_SECONDS = 43200f;
    private const float SUNSET_TIME_SECONDS = 64800f;
    private const float STARS_APPEAR_TIME_SECONDS = 86400f - 3600;


    [SerializeField] Material skyboxMaterial;
    [SerializeField] Light directionalLight;

    [Header("Time Parameters")]
    [SerializeField] float timeScale;
    [SerializeField] Vector3 simulationStartTimeHMS;
    private float currentTime;


    [Header("Sun Parameters")]
    [SerializeField] float sunRotationAngle;  //in degrees

    [SerializeField] float sunriseThckness;

    [SerializeField] float sunriseIntensity;
    [SerializeField] float noonIntensity;

    [SerializeField] Color sunriseColor;
    [SerializeField] Color noonColor;
    [SerializeField] Color sunsetColor;
    [SerializeField] Color midnightColor;

    [Header("Star Parameters")]
    [SerializeField] Transform starTransform;
    [SerializeField] float starRotationAngle; //in degrees
    [SerializeField] float starRotationSpeed; //in seconds

    //sun private parameters
    private float thicknessChangeRate;
    private float exposureChangeRate;
    private Vector3 sunRotationAxis;

    //star private parameters
    private float starFade;
    private Color starFadeColor = new Color(.5f, .5f, .5f, .5f);
    private Vector3 starRotationAxis;
    private Renderer starRenderer;


    // Properties for skybox material
    float SunSize
    {
        get { return skyboxMaterial.GetFloat("_SunSize"); }
        set { skyboxMaterial.SetFloat("_SunSize", value); }
    }
    float AtmoshphereThickness
    {
        get { return skyboxMaterial.GetFloat("_AtmosphereThickness"); }
        set { skyboxMaterial.SetFloat("_AtmosphereThickness", value); }
    }
    Color SkyTint
    {
        get { return skyboxMaterial.GetColor("_SkyTint"); }
        set { skyboxMaterial.SetColor("_SkyTint", value); }
    }
    float Intensity
    {
        get { return directionalLight.intensity; }
        set { directionalLight.intensity = value; }
    }
    Color SunColor
    {
        get { return directionalLight.color; }
        set { directionalLight.color = value; }
    }
    Color FogColor
    {
        get { return RenderSettings.fogColor; }
        set { RenderSettings.fogColor = value; }
    }
    float FogDensity
    {
        get { return RenderSettings.fogDensity; }
        set { RenderSettings.fogDensity = value; }
    }
    Color StarColor
    {
        get { return starRenderer.material.GetColor("_TintColor"); }
        set { starRenderer.material.SetColor("_TintColor", value); }
    }

    // Start is called before the first frame update
    void Start()
    {
        //get components
        starRenderer = starTransform.GetComponent<ParticleSystem>().GetComponent<Renderer>();

        //calculate middleman variables
        sunRotationAxis = new(Mathf.Cos(Mathf.Deg2Rad * sunRotationAngle), Mathf.Sin(Mathf.Deg2Rad * sunRotationAngle), 0);
        starRotationAxis = new(Mathf.Cos(Mathf.Deg2Rad * starRotationAngle), Mathf.Sin(Mathf.Deg2Rad * starRotationAngle), 0);

        //set initial parmater values
        //sun
        float startTime = HMS_to_seconds(simulationStartTimeHMS);
        currentTime = startTime;
        directionalLight.transform.rotation = GetRotationAtTime(startTime);
        AtmoshphereThickness = GetThicknessAtTime(startTime);
        Intensity = GetIntensityAtTime(startTime);
        SunColor = GetColorAtTime(startTime);

        //stars
        starFade = GetStarFadeAtTime(startTime);
        starFadeColor.a = starFade;
        StarColor = starFadeColor;
    }

    // Update is called once per frame
    void Update()
    {
        //update time
        currentTime += Time.deltaTime * timeScale;
        if (currentTime > DAYLENGTH_SECONDS)
        {
            currentTime = 0;
        }

        //Controls
        if (Input.GetKeyDown(KeyCode.R))
        {
            Start();
        }

        //Automate sun parameters
        directionalLight.transform.rotation = GetRotationAtTime(currentTime);
        AtmoshphereThickness = GetThicknessAtTime(currentTime);
        Intensity = GetIntensityAtTime(currentTime);
        SunColor = GetColorAtTime(currentTime);

        //Star parameters
        starTransform.Rotate(starRotationAxis, starRotationSpeed * Time.deltaTime);
        starFade = GetStarFadeAtTime(currentTime);
        starFadeColor.a = starFade;
        StarColor = starFadeColor;
    }

    private Quaternion GetRotationAtTime(float time)
    {
        return Quaternion.AngleAxis((270 + 360 / DAYLENGTH_SECONDS * time) % 360, sunRotationAxis);
    }

    private float GetThicknessAtTime(float time)
    {
        float buffer_seconds = 3600f;
        float dy = sunriseThckness - 1;

        if (time < SUNRISE_TIME_SECONDS - buffer_seconds) return 1;
        else if (time < SUNRISE_TIME_SECONDS) return 1 + dy * ((time - SUNRISE_TIME_SECONDS + buffer_seconds) / buffer_seconds);
        else if (time < SUNRISE_TIME_SECONDS + buffer_seconds) return 1 + dy * ((-time + SUNRISE_TIME_SECONDS + buffer_seconds) / buffer_seconds);
        else if (time < SUNSET_TIME_SECONDS - buffer_seconds) return 1;
        else if (time < SUNSET_TIME_SECONDS) return 1 + dy * ((time - SUNSET_TIME_SECONDS + buffer_seconds) / buffer_seconds);
        else if (time < SUNSET_TIME_SECONDS + buffer_seconds) return 1 + dy * ((-time + SUNSET_TIME_SECONDS + buffer_seconds) / buffer_seconds);
        else return 1;
    }

    private float GetIntensityAtTime(float time)
    {
        float buffer_seconds = 720f;
        float dy = noonIntensity - sunriseIntensity;

        if (time < SUNRISE_TIME_SECONDS - buffer_seconds) return 0;
        else if (time < SUNRISE_TIME_SECONDS) return sunriseIntensity * ((time - SUNRISE_TIME_SECONDS + buffer_seconds) / buffer_seconds);
        else if (time < NOON_TIME_SECONDS) return sunriseIntensity + dy * ((time - SUNRISE_TIME_SECONDS) / SUNRISE_TIME_SECONDS);
        else if (time < SUNSET_TIME_SECONDS) return noonIntensity - dy * ((time - NOON_TIME_SECONDS) / SUNRISE_TIME_SECONDS);
        else if (time < SUNSET_TIME_SECONDS + buffer_seconds) return sunriseIntensity * ((-time + SUNSET_TIME_SECONDS + buffer_seconds) / buffer_seconds);
        else return 0;
    }

    private Color GetColorAtTime(float time)
    {
        float buffer_seconds = 5400f; //1.5hr
        float dy = noonIntensity - sunriseIntensity;

        if (time < SUNRISE_TIME_SECONDS - buffer_seconds) return midnightColor;
        else if (time < SUNRISE_TIME_SECONDS) return Color.Lerp(midnightColor, sunriseColor, (time - SUNRISE_TIME_SECONDS + buffer_seconds) / buffer_seconds);
        else if (time < SUNRISE_TIME_SECONDS + buffer_seconds) return Color.Lerp(sunriseColor, noonColor, (time - SUNRISE_TIME_SECONDS) / buffer_seconds);
        else if (time < SUNSET_TIME_SECONDS - buffer_seconds) return noonColor;
        else if (time < SUNSET_TIME_SECONDS) return Color.Lerp(noonColor, sunsetColor, (time - SUNSET_TIME_SECONDS + buffer_seconds) / buffer_seconds);
        else if (time < SUNSET_TIME_SECONDS + buffer_seconds) return Color.Lerp(sunsetColor, midnightColor, (time - SUNSET_TIME_SECONDS) / buffer_seconds);
        else return midnightColor;
    }

    private float GetStarFadeAtTime(float time)
    {
        if (time < STARS_DISAPPEAR_TIME_SECONDS) return 1;
        else if (time < SUNRISE_TIME_SECONDS) return 1 - (time - STARS_DISAPPEAR_TIME_SECONDS) / (SUNRISE_TIME_SECONDS - STARS_DISAPPEAR_TIME_SECONDS);
        else if (time < SUNSET_TIME_SECONDS) return 0;
        else if (time < STARS_APPEAR_TIME_SECONDS) return (time - SUNSET_TIME_SECONDS) / (STARS_APPEAR_TIME_SECONDS - SUNSET_TIME_SECONDS);
        else return 1;
    }

    private float HMS_to_seconds(Vector3 time)
    {
        return time.x * 3600 + time.y * 60 + time.z;
    }
}

