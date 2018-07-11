using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/**
 * PlanetManager is responsible for spawning and controlling the PerlinPlanet, telling it what to do, how to move, and when
 * to regenerate its surface - particularly in response to user input. It connects with all UI elements, and through it all 
 * UI elements connect to the PerlinPlanet.
 */
public class PlanetManager : MonoBehaviour
{
    public Transform prefab;            // The prefab for the PerlinPlanet
    public Transform rotationPoint;     // The point relative to which the PerlinPlanet rotates
    
    public Dropdown noiseSelector;      // Selector for Perlin vs. Value noise
    public InputField seedField;        // Field containing the seed to generate from
    public Slider rotationSpeedSlider;  // Slider controlling planet's rotation speed
    public Toggle togglePoles;          // Toggle for the pole display
    public Slider xRotationSlider;      // Slider controlling x-axis of viewing angle
    public Slider yRotationSlider;      // Slider controlling y-axis of viewing angle
    private PerlinPlanet planet;        // Reference to the PerlinPlanet itself

    /**
     * Spawns the PerlinPlanet and sets the angle of the rotation point
     */
    void Start ()
    {
        // Set the seed of the RNG to the value of the seed field BEFORE instantiating the planet
        PerlinPlanet prePlanet = prefab.GetComponent<PerlinPlanet>();
        prePlanet.seed = (int)Random.Range(int.MinValue, int.MaxValue);
        seedField.text = prePlanet.seed.ToString();

        // Instantiate the planet and make it a child of the rotation point
        GameObject planetObject = Instantiate(prefab.gameObject);
        planet = planetObject.GetComponent<PerlinPlanet>();
        planetObject.transform.SetParent(rotationPoint);

        // Set angle of rotation point according to initial slider positions
        SetRotationPointAngle();
    }
	
    /**
     * Initiate the planet surface generation process, shrinking the mesh down to size 0 before
     * creating a new surface from the seed in the seedfield and restoring its size.
     */
    public void GeneratePlanet()
    {
        int seed = int.Parse(seedField.text);
        planet.ResetPlanet(seed);
    }

    /**
     *  Sets the PerlinPlanet's noise type according to 
     */
    public void SetNoiseMethod()
    {
        if (noiseSelector.value == 0)
            planet.noiseType = NoiseMethodType.Perlin;
        else
            planet.noiseType = NoiseMethodType.Value;
    }

    /**
     * Generates a random int, converts it to a String, and writes it into the seed field
     */
    public void SetRandomSeed()
    {
        int newSeed = (int)Random.Range(int.MinValue, int.MaxValue);
        seedField.text = newSeed.ToString();
    }

    /**
     * Sets the speed at which the PerlinPlanet rotates around its y-axis
     */
    public void SetRotationalSpeed()
    {
        planet.targetRotationalSpeed = rotationSpeedSlider.value * 5;
    }

    /**
     * Hides the pole display if it is on, or reveals it if it is off
     */
    public void TogglePoles()
    {
        planet.poles.SetActive(togglePoles.isOn);
    }

    /**
     * Exits the game
     */
    public void ExitProgram()
    {
        Application.Quit();
    }

    /**
     * Sets the angle of the rotation point according to the values of the x- and y-sliders
     */
    public void SetRotationPointAngle()
    {
        Vector3 currentAngle = rotationPoint.localEulerAngles;
        currentAngle.y = (1 - yRotationSlider.value) * 360f - 180f;

        currentAngle.x = 180 * xRotationSlider.value - 90f - 7.3f;
        currentAngle.z = 0;

        rotationPoint.transform.rotation = Quaternion.Euler(currentAngle);

    }
}
