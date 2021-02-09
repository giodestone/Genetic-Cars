using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Random = System.Random;

public struct BenchData
{
    public struct CarData
    {
        public float TotalArea;
        public float Speed;
    }

    public int Generation;
    public float[] CarFitnesses;
    public float[] CarSizes;
    public CarData BestCarOfGeneration;
}

public class CarManager : MonoBehaviour
{
    /// <summary>
    /// For storing all the benchmark states.
    /// </summary>
    enum CurrentBench
    {
        Fitness1,
        Fitness2,

        Fitness1SmallSizeReward,
        Fitness2SmallSizeReward,

        Fitness1BigSizeReward,
        Fitness2BigSizeReward,

        BencFinished,
        DONT_RUN
    }

    /// <summary>
    /// Size reward state.
    /// </summary>
    enum SizeReward
    {
        DontReward,
        RewardBig,
        RewardSmall
    }

    [SerializeField] private int PopulationSize = 20;

    private GeneticAlgorithm<float> GeneticAglorithm;
    System.Random Random = new Random();

    // Prefabs
    [SerializeField] private GameObject TerrainPrefab;
    [SerializeField] private GameObject DefaultCarPrefab;
    [SerializeField] private GameObject RootGameObjectToAttachTerrainTo;

    // Cars
    private List<GameObject> Cars;

    // To do with simulation.
    private float TimeStartedIteraion = float.PositiveInfinity;
    private const float MaxTimeForIteration = 100f;

    private bool ShouldBeRunning = false;
    private bool CheckIfCarsAreUpright = false;

    private Slider TimeScaleSlider;
    private float TimeScale = 1f;

    // To do with GUI
    private TextMeshProUGUI GenerationText;
    private TextMeshProUGUI FitnessText;
    private TextMeshProUGUI BenchText;
    private TextMeshProUGUI RunText;

    private TMP_Dropdown FitnessDropdown;
    private TMP_Dropdown SizeRewardDropdown;

    // For benchmarks
    private List<BenchData> BenchData;
    private SizeReward CurrentlyRewardingSize;
    private bool HasAnyCarReachedGoal = false;

    private CurrentBench CurrentBenchmarkBeingRan = CurrentBench.BencFinished;
    private const int RunsPerBench = 4;
    private int Run = 0;

    /// <summary>
    /// Initialise all the variables (get references etc.), generate an initial scene.
    /// </summary>
    void Start()
    {
        // Verfiy that prefabs are set.
        Assert.IsNotNull(DefaultCarPrefab, "Car prefab not set!");
        Assert.IsNotNull(TerrainPrefab, "Terrain prefab not set!");
        Assert.IsNotNull(RootGameObjectToAttachTerrainTo, "RootGameObjectToAttachTerrainTo not set!");

        // Get text components.
        GenerationText = GameObject.Find("Generation Text").GetComponent<TextMeshProUGUI>();
        FitnessText = GameObject.Find("Fitness Text").GetComponent<TextMeshProUGUI>();
        BenchText = GameObject.Find("Bench Text").GetComponent<TextMeshProUGUI>();
        RunText = GameObject.Find("Run Text").GetComponent<TextMeshProUGUI>();
        TimeScaleSlider = GameObject.Find("Time Scale Slider").GetComponent<Slider>();
        
        Assert.IsNotNull(TimeScaleSlider, "Not found time scale slider by game object name!");

        FitnessDropdown = GameObject.Find("Fitness Dropdown").GetComponent<TMP_Dropdown>();
        SizeRewardDropdown = GameObject.Find("Size Reward Dropdown").GetComponent<TMP_Dropdown>();

        CurrentBenchmarkBeingRan = CurrentBench.DONT_RUN;
        CurrentlyRewardingSize = SizeReward.DontReward;
        Initialise(FitnessFunction);
    }

    /// <summary>
    /// Initialise the simulation, spawn cars and levels, and destroy old ones.
    /// </summary>
    /// <param name="fitnessFunction">Fitness function to use.</param>
    private void Initialise(Func<int, float> fitnessFunction)
    {
        // Get rid of all children from root object if there are any.
        List<GameObject> childRootGameObjects = new List<GameObject>(RootGameObjectToAttachTerrainTo.transform.childCount);
        for (int childIndex = 0; childIndex < RootGameObjectToAttachTerrainTo.transform.childCount; childIndex++)
            childRootGameObjects.Add(RootGameObjectToAttachTerrainTo.transform.GetChild(childIndex).gameObject);
        
        childRootGameObjects.ForEach(Destroy);

        // Initialise algorithm and stuff again.
        GeneticAglorithm = new GeneticAlgorithm<float>(PopulationSize, Car.NoOfGenes, Random, GetRandomGene, fitnessFunction, PopulationSize / 10, mutationRate: 0.05f);
        CheckIfCarsAreUpright = GeneticAglorithm.FitnessFunction == FitnessFunction2NoFlip;
        Cars = new List<GameObject>();

        // Instanciate new cars.
        for (int i = 0; i < PopulationSize; ++i)
        {
            var newTerrain = Instantiate(TerrainPrefab, RootGameObjectToAttachTerrainTo.transform);
            var newCar = Instantiate(DefaultCarPrefab, newTerrain.transform);
            newTerrain.transform.position = new Vector3(0f, -20f * (float)i, 0f);

            newCar.transform.localPosition = new Vector3(0f, 5f, 0f);

            newCar.GetComponent<Car>().ShouldBeTrackingUpright = CheckIfCarsAreUpright;

            var newGoal = newTerrain.GetComponentInChildren<Goal>();
            newCar.GetComponent<Car>().GoalPosition = newGoal.transform.position;

            Cars.Add(newCar);
        }
    }

    /// <summary>
    /// Fitness Function 1, which doesn't take into account the flipping.
    /// </summary>
    /// <param name="index">Current car index.</param>
    /// <returns>Fitness.</returns>
    private float FitnessFunction(int index)
    {
        var car = Cars[index].GetComponent<Car>();
        Assert.IsNotNull(car, "Unable to find car component.");
        var fitness = (car.GoalPosition - car.StartPosition).magnitude - (car.GoalPosition - car.EndPosition).magnitude; // Calculate distance

        if (car.HasReachedGoal)
            fitness *= 2f;

        fitness += car.MaxSpeedReached * 4f;

        fitness += RewardSize(car, CurrentlyRewardingSize);

        return fitness; // Fitness is the distance towards distance.
    }

    /// <summary>
    /// Second fitness function, which penalizes the car if it flips.
    /// </summary>
    /// <param name="index">Current car index.</param>
    /// <returns>Fitness</returns>
    private float FitnessFunction2NoFlip(int index)
    {
        var car = Cars[index].GetComponent<Car>();
        Assert.IsNotNull(car, "Unable to find car component.");

        var distance = (car.GoalPosition - car.StartPosition).magnitude - (car.GoalPosition - car.EndPosition).magnitude;

        if (car.HasReachedGoal) distance *= 2f; // Reward cars that get to the goal.
        if (car.HasReachedFlippingPoint) distance /= 4f; // Punish cars that flip over.

        distance += car.MaxSpeedReached * 4f; // Reward speed.

        distance += RewardSize(car, CurrentlyRewardingSize);

        return distance + 1f; //need to be above 0 not to crash. 
    }

    /// <summary>
    /// Function for calculating the reward size.
    /// </summary>
    /// <param name="car">Car to check area of.</param>
    /// <param name="rewardSize">What to reward.</param>
    /// <returns>Reward based off of rewardSize.</returns>
    private float RewardSize(Car car, SizeReward rewardSize)
    {
        // Calculate area
        var totalArea = GetCarArea(car);

        // Penalize, reward, or do nothing based on setting.
        switch (rewardSize)
        {
            case SizeReward.RewardBig:
                return totalArea * 2f;

            case SizeReward.RewardSmall:
                return -(totalArea * 2f);

            default:
            case SizeReward.DontReward:
                return 0f;
        }

    }

    /// <summary>
    /// Calculate the cars area.
    /// </summary>
    /// <param name="car">Area of car to calculate.</param>
    /// <returns>Area of car in units squared.</returns>
    private float GetCarArea(Car car)
    {
        var areaOfBase = car.BaseOfCar.GetComponent<BoxCollider2D>().bounds.size.x *
                         car.BaseOfCar.GetComponent<BoxCollider2D>().bounds.size.y;
        var areaOfLeftWheel = Mathf.PI * car.LeftWheel.GetComponent<CircleCollider2D>().radius;
        var areaOfRightWheel = Mathf.PI * car.RightWheel.GetComponent<CircleCollider2D>().radius;

        return areaOfBase + areaOfLeftWheel + areaOfRightWheel;
    }

    /// <summary>
    /// Get random gene function which takes into account which gene it is, and calculates the according value.
    /// </summary>
    /// <param name="currentGene">Index of current gene.</param>
    /// <returns>A random value which is modified based off of the currentGene.</returns>
    private float GetRandomGene(int currentGene)
    {
        var enumValues = Enum.GetValues(typeof(GeneType)) as GeneType[];
        var noOfParamaters = enumValues.Length;
        var genesPerParamater = Car.NoOfGenes / noOfParamaters;

        var currentGeneParamIndex = -1;

        // Get which current gene this is on.
        for (int i = 1; i < noOfParamaters + 1; ++i)
        {
            int paramStartIndex = (i - 1) * genesPerParamater;
            int paramEndIndex = i * genesPerParamater;

            if (currentGene >= paramStartIndex && currentGene <= paramEndIndex)
            {
                currentGeneParamIndex = i - 1;
            }
        }

        // Return a value based off of the gene.
        var currentIndexGene = enumValues[currentGeneParamIndex];

        switch (currentIndexGene)
        {
            case GeneType.LeftWheelPosX:
            case GeneType.LeftWheelPosY:
            case GeneType.RightWheelPosX:
            case GeneType.RightWheelPosY:
                return (float) Random.NextDouble() / genesPerParamater; // Should be a percentage from left (i.e. 0 to 1)

            case GeneType.Speed:
                return (float)Random.NextDouble() * 30f; // Don't make the car too slow.

            default:
                return (float)Random.NextDouble();
        }

    }

    void Update()
    {
        // Update time scale from slider.
        Time.timeScale = TimeScaleSlider.value;

        // Update current benchmark state.
        if (CurrentBenchmarkBeingRan != CurrentBench.DONT_RUN)
            RunBenchmarks();

        if ((Cars.TrueForAll(x => x.GetComponent<Car>().HasRanSimulation) || Time.time > TimeStartedIteraion + MaxTimeForIteration) 
            && ShouldBeRunning)
        {
            // Check if any has reached goal.
            if (Cars.Any(car => car.GetComponent<Car>().HasReachedGoal) && CurrentBenchmarkBeingRan != CurrentBench.DONT_RUN)
            {
                this.PauseRun();
                CarReachedGoal();
                return;
            }

            // End simulation if not ended already.
            Cars.ForEach(car => car.GetComponent<Car>().EndSimulation());

            TimeStartedIteraion = Time.time;
            GeneticAglorithm.NewGeneration();

            if (CurrentBenchmarkBeingRan != CurrentBench.DONT_RUN)
                AddStatsOfCurrentGenerationToBench();

            ApplyGenesToCars();
            Cars.ForEach(car => car.GetComponent<Car>().StartSimulation());

            // Update Text
            GenerationText.text = "Generation: " + GeneticAglorithm.Generation.ToString();
            FitnessText.text = "Best Fitness: " + GeneticAglorithm.BestFitness.ToString();
        }
    }

    /// <summary>
    /// Convert the gene into variables that the car can use, and apply to its components.
    /// </summary>
    void ApplyGenesToCars()
    {
        for (int i = 0; i < PopulationSize; ++i)
        {
            var currentCar = Cars[i].GetComponent<Car>();
            var currentGene = GeneticAglorithm.Population[i].Genes;

            for (int j = 0; j < currentCar.GenesToValues.Keys.Count; j++)
            {
                var step = currentGene.Length / currentCar.GenesToValues.Count; // Calculate no of genes per variable.

                currentCar.GenesToValues[currentCar.GenesToValues.Keys.ToArray()[j]] = CalcGene(currentGene, j * step, ((j + 1) * step) - 1);
            }
        }
    }

    /// <summary>
    /// Calculate the value of a variable from an array of index, based off of the start and end index.
    /// </summary>
    /// <param name="genes"></param>
    /// <param name="startIndexInclusive"></param>
    /// <param name="endIndexInclusive"></param>
    /// <returns>Resulting value of genes between startIndexInclusive and endIndexInclusive.</returns>
    float CalcGene(float[] genes, int startIndexInclusive, int endIndexInclusive)
    {
        float returnAmount = 0f;

        for (int i = startIndexInclusive; i <= endIndexInclusive; ++i)
        {
            returnAmount += genes[i];
        }

        return returnAmount;
    }

    /// <summary>
    /// Start running.
    /// </summary>
    public void StartRunning()
    {
        ShouldBeRunning = true;
    }

    /// <summary>
    /// Pause the run.
    /// </summary>
    public void PauseRun()
    {
        ShouldBeRunning = false;
    }

    /// <summary>
    /// What to do when a car has reached a goal.
    /// </summary>
    private void CarReachedGoal()
    {
        HasAnyCarReachedGoal = true;
    }

    /// <summary>
    /// Benchmarking function.
    /// </summary>
    /// <param name="firstRun">Whether this is the first time the benchmark is being ran.</param>
    private void RunBenchmarks(bool firstRun=false)
    {
        if (HasAnyCarReachedGoal || firstRun)
        {
            if (!firstRun)
            {
                Run++;
            }

            RunText.text = "Run: " + Run + "/" + RunsPerBench;

            PauseRun();

            if (!firstRun && Run >= RunsPerBench)
            {
                SaveToSpreadsheet();
            }

            switch (CurrentBenchmarkBeingRan)
            {
                case CurrentBench.Fitness1:
                    BenchText.text = "Bench: " + CurrentBenchmarkBeingRan.ToString();
                    if (!firstRun && Run >= RunsPerBench)
                    {
                        CurrentBenchmarkBeingRan = CurrentBench.Fitness2;
                        Run = 0;
                        RunText.text = "Run: " + Run + "/" + RunsPerBench;
                        BenchData = new List<BenchData>();
                        goto case CurrentBench.Fitness2; // The system works one behind, so if its not the first run chances its already been initailised.
                    }
                    CurrentlyRewardingSize = SizeReward.DontReward;
                    Initialise(FitnessFunction);
                    break;
                case CurrentBench.Fitness2:
                    BenchText.text = "Bench: " + CurrentBenchmarkBeingRan.ToString();
                    CurrentlyRewardingSize = SizeReward.DontReward;
                    Initialise(FitnessFunction2NoFlip);
                    if (Run >= RunsPerBench)
                    {
                        CurrentBenchmarkBeingRan = CurrentBench.Fitness1SmallSizeReward;
                        BenchData = new List<BenchData>();
                        Run = 0;
                        RunText.text = "Run: " + Run + "/" + RunsPerBench;
                    }
                    break;

                case CurrentBench.Fitness1SmallSizeReward:
                    BenchText.text = "Bench: " + CurrentBenchmarkBeingRan.ToString();
                    CurrentlyRewardingSize = SizeReward.RewardSmall;
                    Initialise(FitnessFunction);
                    if (Run >= RunsPerBench)
                    {
                        CurrentBenchmarkBeingRan = CurrentBench.Fitness2SmallSizeReward;
                        BenchData = new List<BenchData>();
                        Run = 0;
                        RunText.text = "Run: " + Run + "/" + RunsPerBench;
                    }
                    break;
                case CurrentBench.Fitness2SmallSizeReward:
                    BenchText.text = "Bench: " + CurrentBenchmarkBeingRan.ToString();
                    CurrentlyRewardingSize = SizeReward.RewardSmall;
                    Initialise(FitnessFunction2NoFlip);
                    if (Run >= RunsPerBench)
                    {
                        CurrentBenchmarkBeingRan = CurrentBench.Fitness1BigSizeReward;
                        BenchData = new List<BenchData>();
                        Run = 0;
                        RunText.text = "Run: " + Run + "/" + RunsPerBench;
                    }
                    break;

                case CurrentBench.Fitness1BigSizeReward:
                    BenchText.text = "Bench: " + CurrentBenchmarkBeingRan.ToString();
                    CurrentlyRewardingSize = SizeReward.RewardBig;
                    Initialise(FitnessFunction);
                    if (Run >= RunsPerBench)
                    {
                        CurrentBenchmarkBeingRan = CurrentBench.Fitness2BigSizeReward;
                        BenchData = new List<BenchData>();
                        Run = 0;
                        RunText.text = "Run: " + Run + "/" + RunsPerBench;
                    }
                    break;
                case CurrentBench.Fitness2BigSizeReward:
                    BenchText.text = "Bench: " + CurrentBenchmarkBeingRan.ToString();
                    CurrentlyRewardingSize = SizeReward.RewardBig;
                    Initialise(FitnessFunction2NoFlip);
                    if (Run >= RunsPerBench)
                    {
                        CurrentBenchmarkBeingRan = CurrentBench.BencFinished;
                        BenchData = new List<BenchData>();
                        Run = 0;
                        RunText.text = "Run: " + Run + "/" + RunsPerBench;
                    }
                    break;

            }

            if (CurrentBenchmarkBeingRan == CurrentBench.BencFinished)
            {
                CurrentBenchmarkBeingRan = CurrentBench.DONT_RUN;
                PauseRun();
                return;
            }

            HasAnyCarReachedGoal = false;
            StartRunning();
        }
    }

    /// <summary>
    /// Add the values of the current generation to the csv file.
    /// </summary>
    private void AddStatsOfCurrentGenerationToBench()
    {
        // Get fitnesses for each car
        var carFitnesses = new List<float>(PopulationSize);
        var carSizes = new List<float>(PopulationSize);

        foreach (var dna in GeneticAglorithm.PreviousPopulation) // Previous population is current generation.
        {
            carFitnesses.Add(dna.Fitness);
        }

        foreach (var car in Cars)
        {
            carSizes.Add(GetCarArea(car.GetComponent<Car>()));
        }

        // Get best car.
        var bestCar = Cars[GeneticAglorithm.BestFitnessIndex].GetComponent<Car>();
        
        BenchData.CarData bestCarData = new BenchData.CarData();
        bestCarData.Speed = bestCar.Speed;
        bestCarData.TotalArea = GetCarArea(bestCar);

        // Populate bench data
        var benchData = new BenchData();
        benchData.Generation = GeneticAglorithm.Generation;
        benchData.CarFitnesses = carFitnesses.ToArray();
        benchData.CarSizes = carSizes.ToArray();
        benchData.BestCarOfGeneration = bestCarData;

        // Store it for later use
        BenchData.Add(benchData);
    }

    /// <summary>
    /// Save the collated values to a spreadsheet based off of the current value.
    /// </summary>
    private void SaveToSpreadsheet()
    {
        // Save to csv (csv!)
        var csvString = new StringBuilder();

        // Add title
        var title = "Generation (Fitness),";

        for (var i = 0; i < BenchData[0].CarFitnesses.Length; i++)
        {
            var carNo = string.Format("Car {0},", i);
            title += carNo;
        }

        title += ", Generation (Area),";

        for (var i = 0; i < BenchData[0].CarSizes.Length; i++)
        {
            var carSize = string.Format("Car {0} Area,", i);
            title += carSize;
        }

        csvString.AppendLine(title);

        // Populate data
        foreach (var benchData in BenchData)
        {
            // Start with generation
            var carFitnesses = benchData.Generation.ToString() + ",";
            var carSizes = "," + benchData.Generation.ToString() + ","; // Preappend a comma so a row between the two gets left out.

            // Append all cars.
            for (var i = 0; i < benchData.CarFitnesses.Length; i++)
            {
                carFitnesses += benchData.CarFitnesses[i].ToString() + ",";
                carSizes += benchData.CarSizes[i].ToString() + ",";
            }

            // Finally add line to csv.
            csvString.AppendLine(carFitnesses  + carSizes);
        }

        File.WriteAllText(CurrentBenchmarkBeingRan.ToString() + "_" + PopulationSize + ".csv", csvString.ToString());
    }

    /// <summary>
    /// What to do when initialise button is pressed.
    /// </summary>
    public void InitialiseButton()
    {
        Func<int, float> fitnessFunc = null;
        CurrentBenchmarkBeingRan = CurrentBench.DONT_RUN;
        // Get Fitness function.
        switch (FitnessDropdown.value)
        {
            case 0:
                fitnessFunc = FitnessFunction;
                break;
            case 1:
                fitnessFunc = FitnessFunction2NoFlip;
                break;
        }


        // Get size reward
        switch (SizeRewardDropdown.value)
        {
            case 0:
                CurrentlyRewardingSize = SizeReward.DontReward;
                break;
            case 1:
                CurrentlyRewardingSize = SizeReward.RewardBig;
                break;
            case 2: 
                CurrentlyRewardingSize = SizeReward.RewardSmall;
                break;
        }

        // Finally initialise
        PauseRun();
        Initialise(fitnessFunc);
    }

    /// <summary>
    /// What to do when the run benchmarks button is pressed.
    /// </summary>
    public void OnRunBenchmarks()
    {
        PauseRun();
        // Start benchmark.
        BenchData = new List<BenchData>();
        CurrentBenchmarkBeingRan = CurrentBench.Fitness1;
        RunBenchmarks(true);
    }
}
