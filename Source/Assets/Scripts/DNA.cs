using System;

public class DNA<T>
{
    public T[] Genes { get; private set; } // Individual genes.
    public float Fitness { get; private set; } // Overall fitness, calculated before making a new generation.

    private Random random;
    private Func<int, T> getRandomGene;
    private Func<int, float> fitnessFunction;

    /// <summary>
    /// Create a DNA, which represents a car.
    /// </summary>
    /// <param name="size">Amount of genes.</param>
    /// <param name="random"></param>
    /// <param name="getRandomGene">Random gene function.</param>
    /// <param name="fitnessFunction">Fitness function.</param>
    /// <param name="shouldInitGenes">Whether to initialise the genes with the getRandomGenes function.</param>
    public DNA(int size, Random random, Func<int, T> getRandomGene, Func<int, float> fitnessFunction, bool shouldInitGenes = true)
    {
        Genes = new T[size];
        this.random = random;
        this.getRandomGene = getRandomGene;
        this.fitnessFunction = fitnessFunction;

        if (shouldInitGenes)
        {
            for (int i = 0; i < Genes.Length; i++)
            {
                Genes[i] = getRandomGene(i);
            }
        }
    }

    public float CalculateFitness(int index)
    {
        Fitness = fitnessFunction(index);
        return Fitness;
    }

    /// <summary>
    /// Randomly cross over the parent and this, generating a new child.
    /// </summary>
    /// <param name="otherParent"></param>
    /// <returns></returns>
    public DNA<T> Crossover(DNA<T> otherParent)
    {
        DNA<T> child = new DNA<T>(Genes.Length, random, getRandomGene, fitnessFunction, shouldInitGenes: false);

        for (int i = 0; i < Genes.Length; i++)
        {
            child.Genes[i] = random.NextDouble() < 0.5 ? Genes[i] : otherParent.Genes[i];
        }

        return child;
    }

    /// <summary>
    /// Mutate some genes, according to the mutation rate.
    /// </summary>
    /// <param name="mutationRate">Probability of mutating gene (0: none, 1: 100%)</param>
    public void Mutate(float mutationRate)
    {
        for (int i = 0; i < Genes.Length; i++)
        {
            if (random.NextDouble() < mutationRate)
            {
                Genes[i] = getRandomGene(i);
            }
        }
    }
}