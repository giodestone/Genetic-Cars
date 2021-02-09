using System;
using System.Collections.Generic;

public class GeneticAlgorithm<T>
{
	public List<DNA<T>> Population { get; private set; } // The new population.
	public List<DNA<T>> PreviousPopulation { get; private set; } // The population from the last run.
	public int Generation { get; private set; } // Which generation is this.
	public float BestFitness { get; private set; } // Fittest car in the population.
	public int BestFitnessIndex { get; private set; } // The index in the population of the fittest gene.
	public T[] BestGenes { get; private set; } // The most elite genes of the generation.

	public int Elitism; // Amount of 'elite' i.e. best of generation.
	public float MutationRate; // How frequently should a gene be mutated.

	private List<DNA<T>> newPopulation;
	private Random random;
	private float fitnessSum;
	private int dnaSize;
	private Func<int, T> getRandomGene;
	private Func<int, float> fitnessFunction;
	public Func<int, float> FitnessFunction { get => fitnessFunction; }

	/// <summary>
	/// Create a genetic algorithm
	/// </summary>
	/// <param name="populationSize"></param>
	/// <param name="dnaSize">How many genes should a DNA have</param>
	/// <param name="random"></param>
	/// <param name="getRandomGene"></param>
	/// <param name="fitnessFunction"></param>
	/// <param name="elitism"></param>
	/// <param name="mutationRate"></param>
	public GeneticAlgorithm(int populationSize, int dnaSize, Random random, Func<int, T> getRandomGene, Func<int, float> fitnessFunction,
		int elitism, float mutationRate = 0.01f)
	{
		Generation = 1;
		Elitism = elitism;
		MutationRate = mutationRate;
		Population = new List<DNA<T>>(populationSize);
		newPopulation = new List<DNA<T>>(populationSize);
		this.random = random;
		this.dnaSize = dnaSize;
		this.getRandomGene = getRandomGene;
		this.fitnessFunction = fitnessFunction;

		BestGenes = new T[dnaSize];

		for (int i = 0; i < populationSize; i++)
		{
			Population.Add(new DNA<T>(dnaSize, random, getRandomGene, fitnessFunction, shouldInitGenes: true));
		}
	}

	/// <summary>
	/// Calculate fitness, crossbreed DNA with eachother and generate a new generation ready to be tested.
	/// </summary>
	/// <param name="numNewDNA">How many new members into the population should be introduced.</param>
	/// <param name="crossoverNewDNA">Whether the new DNA should crossover with the rest.</param>
	public void NewGeneration(int numNewDNA = 0, bool crossoverNewDNA = false)
	{
		int finalCount = Population.Count + numNewDNA;

		if (finalCount <= 0)
		{
			return;
		}

		if (Population.Count > 0)
		{
			CalculateFitness();
			Population.Sort(CompareDNA);
		}
		newPopulation.Clear();

		for (int i = 0; i < Population.Count; i++)
		{
			if (i < Elitism && i < Population.Count)
			{
				newPopulation.Add(Population[i]);
			}
			else if (i < Population.Count || crossoverNewDNA)
			{
				DNA<T> parent1 = ChooseParent();
				DNA<T> parent2 = ChooseParent();

				DNA<T> child = parent1.Crossover(parent2);

				child.Mutate(MutationRate);

				newPopulation.Add(child);
			}
			else
			{
				newPopulation.Add(new DNA<T>(dnaSize, random, getRandomGene, fitnessFunction, shouldInitGenes: true));
			}
		}

		List<DNA<T>> tmpList = Population;
        PreviousPopulation = Population;
        Population = newPopulation;
		newPopulation = tmpList;

		Generation++;
	}

	/// <summary>
	/// Comparator for sorting the DNA.
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	private int CompareDNA(DNA<T> a, DNA<T> b)
	{
		if (a.Fitness > b.Fitness)
		{
			return -1;
		}
		else if (a.Fitness < b.Fitness)
		{
			return 1;
		}
		else
		{
			return 0;
		}
	}

	/// <summary>
	/// Calculate the fitness of the population.
	/// </summary>
	private void CalculateFitness()
	{
		fitnessSum = 0;
		DNA<T> best = Population[0];
        BestFitnessIndex = 0;
        BestFitness = 0f;

		for (int i = 0; i < Population.Count; i++)
		{
			fitnessSum += Population[i].CalculateFitness(i);

			if (Population[i].Fitness > best.Fitness)
			{
				best = Population[i];
                if (Population[i].Fitness > BestFitness)
                {
                    BestFitnessIndex = i;
                    BestFitness = Population[i].Fitness;
                }
            }
		}

		BestFitness = best.Fitness;
		best.Genes.CopyTo(BestGenes, 0);
	}

	/// <summary>
	/// Select a parent.
	/// </summary>
	/// <returns>Selected DNA parent.</returns>
	private DNA<T> ChooseParent()
	{
		// Get a random parent which is similar to the fitness requested.
		double randomNumber = random.NextDouble() * fitnessSum;

		for (int i = 0; i < Population.Count; i++)
		{
			if (randomNumber < Population[i].Fitness)
			{
				return Population[i];
			}

			randomNumber -= Population[i].Fitness;
		}

		return null;
	}
}
