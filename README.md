This is a Unity project which can render arbitrarily sized fields of procedurally generated grass.

random grass positions, sizes, and rotations are calculated using a compute shader (ProceduralGrass.compute), and then rendered with GPU instancing (ProceduralGrass.cs).

The terrain which the grass sits on is a procedurally generated mesh with variable level of detail (ProceduralMesh.cs). It is offset by a heightmap which the grass
will match.

The shader used on individual grass blades (UnlitGrass.shader) supports blending of different colours along the length of the grass blade, as well
as bending and perlin noise based wind sway.

The project supports dynamic level of detail, as grass blades will be rendered with a less detailed mesh at a greater distance from the camera,
as well as frustum culling (FrustumCulling.cs) (grass outside the view of the camera will not be rendered).

There is also a player character which can "mow" grass by moving over it. This is accomplished by placing a camera above the terrain (MowingCamera.cs) and capturing the
position of the player in a texture, which is then sampled in the grass shader.

