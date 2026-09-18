## COSC2527
A Unity project developed for **COSC2527 Games and Artificial Intelligence Techniques** at RMIT University, demonstrating game AI algorithms in a survival game setting. A frog must catch flies while evading snakes patrolling the jungle.

## Algorithms
- **Decision Trees**: Two decision trees and a behavior tree (selector/sequence nodes), switchable at runtime
- **Pathfinding**: A* search on a terrain-weighted grid with a binary-heap open set
- **Steering**: Seek, arrive and flee behaviors with obstacle avoidance
- **Flocking**: Reynolds' boids (separation, cohesion, alignment) for the flies
- **Finite state machines**: Event-driven FSMs for the flies and snakes
- **Swarm detection**: Breadth-first search clustering to locate the nearest fly swarm
- **Connect Four**: Monte Carlo Tree Search with UCT and a transposition table

## Manual Controls
- **Left click**: Switch between human and AI  
- **Right click**: Move the frog to the selected location (human mode)  
- **Space**: Shoot a bubble (human mode)  
- **Press 0**: Decision Tree v1  
- **Press 1**: Decision Tree v2  
- **Press 2**: Behavior Tree
  
---
## Game Objective
- Eat 10 flies to win the game
- Avoid the snake

## Game Mechanics
- **Health**: The frog starts with 3 health and loses one per snake bite or fireball hit. The round restarts on a win or loss, with results tracked on screen.
- **Movement**: Terrain type affects the frog's speed. In human mode, the frog uses A* pathfinding to reach the clicked location.
- **Flies**: Move as a flock and scatter when the frog or a bubble comes close. Eaten flies respawn after a short delay.
- **Snakes**: Driven by a finite state machine. They patrol, chase the frog when it comes within range, retreat after biting, and flee when hit by a bubble.
- **Boss snake** (Boss Mode): Shoots fireballs and takes two bubble hits before retreating.
- **AI frog**: Flees nearby snakes and fireballs, shoots bubbles when threatened, and otherwise hunts the nearest fly or flock.
