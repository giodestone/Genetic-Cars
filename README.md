# Genetic Cars
![Video of cars training](https://raw.githubusercontent.com/giodestone/Genetic-Cars/main/Images/GIF1.gif)

Genetic algorithm which evolves optimal 2D cars that can pass over rough terrain. Made for a module in my university course.

## Running
[Download](https://github.com/giodestone/Genetic-Cars/releases)
* W, A, S, D to look around.
* Q, E to zoom in/out

### Training Cars
1. Press Button on top left 'Start'.
2. Adjust the slider in the middle to make training go faster/slower.
3. Wait until a car reaches the goal.

### Adjusting Parameters
The 'Pause' button prevents advancing to the next generation. Advancement can be resumed by pressing the 'Start' button.

The slider controls how fast the simulation is performed.

The drop down on the top-right can be used to adjust the fitness functions.
* Fitness 1: Base the fitness of a car based on distance covered since start.
* Fitness 2: Like fitness 1, except penalize the car if it tips over.

The dropdown below the fitness selection can be used to add rewards based on the total size achieved.
* No size reward: No points are added to fitness.
* Big size reward: More fitness points are awarded to cars which have a bigger area.
* Small size reward: Less fitness is taken away the smaller the car is.

**After changing the values in either drop-down the 'Initialize' button must be pressed.**

The Run Benchmarks button performs benchmarks and its values are stored in a .csv file.

### Unity Version
Made in Unity 2019.3.0f3, though newer versions should work. Unity's 2D physics and renderer is used, and the logic is contained in partly detached code.

## How It Works
![Still from evolution](https://raw.githubusercontent.com/giodestone/Genetic-Cars/main/Images/Image1.jpg)

Each of the cars' 'genes' are represented by an array of doubles using the `DNA` class. The fitness of the gene is determined by how far it has travelled and whether it tipped over (in which case its fitness is reduced and 'fitness 2' is selected). The least fit members are eliminated and best ones kept, then the genes are crossed over with each-other (random elements are spliced together) to create a new generation which is hopefully better than last.

The cars are represented by a body and two wheels. The size of the body & wheels, position of wheels on body, speed of the wheels are affected by the gene.

![Final Cars](https://raw.githubusercontent.com/giodestone/Genetic-Cars/main/Images/Winning%20Cars.jpg)

The evolutions continues until a car which reaches the goal is found. The algorithm should converge on a solution that looks plausible and is stable enough to cross the track.