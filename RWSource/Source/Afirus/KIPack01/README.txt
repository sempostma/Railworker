# Considerations

To optimize performance, we decided to put as many containers as possible in one texture. This allows users to place the most objects without crashing the game. Unfortunately this makes it so that you cant place individual cargos containers.

# Assumptions

- The game is relatively optimized for atlas textures and reusing/caching resources
- Textures are only loaded when needed
- All Bin files in a Provider/Product (and possibly geo files) are loaded when a scenario starts
- Reskin blueprints are optmized
