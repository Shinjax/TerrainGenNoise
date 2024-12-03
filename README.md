# Unity Terrain Generator

A procedural terrain generation system built in Unity that demonstrates and compares different noise functions (Perlin, Value, Worley, and Simplex) for creating various terrain types.

## Features

- Multiple noise function implementations:
  - Perlin Noise
  - Value Noise
  - Worley Noise
  - Simplex Noise

- Terrain Presets:
  - Mountains
  - Plains
  - Hills
  - Coastal
  - Combined (multi-layer approach)

- Three-Layer Terrain System:
  - Base Layer: Large-scale noise for primary terrain features
  - Medium Layer: Natural undulations
  - Detail Layer: High-frequency details

- Interactive Controls:
  - Real-time parameter adjustment
  - Camera controls for terrain exploration
  - Terrain type switching
  - Performance metrics and analysis

## Getting Started

1. Clone this repository
2. Open the project in Unity (developed with Unity 2022.3 or later)
3. Open the main scene
4. Use the UI controls to experiment with different terrain types and parameters

## Controls

- WASD: Move camera horizontally
- Q/E: Move camera up/down
- Right Mouse Button: Look around
- UI Controls: Adjust terrain parameters and switch between noise types/presets

## License

This project is licensed under the MIT License - see the LICENSE file for details.
