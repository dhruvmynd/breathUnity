FunnelSeamlessShader - Usage Guide
==================================

This shader eliminates UV seams on funnel/tube meshes by using cylindrical projection
instead of traditional UV mapping. Perfect for non-tileable render textures.

Key Features:
- Seamless cylindrical texture projection
- 3D twirl effect that maintains seamlessness
- Full URP lighting with metallic/smoothness
- Transparency support
- Two-sided rendering with normal flip option
- Fresnel rim lighting

Setup Instructions:
1. Apply the shader to your funnel mesh material
2. Assign your render texture to the Main Texture slot
3. Adjust the following key parameters:

Texture Projection Settings:
- Texture Scale: Controls texture tiling (X = around circumference, Y = along height)
- Height Scale Factor: Compensates for funnel taper (increase for wider top)
- Use World Space: Toggle between object/world space projection
- Projection Axis: 0=Y (vertical funnel), 1=X, 2=Z

Twirl Settings:
- Twirl Strength: Amount of twirl distortion
- Twirl Center: World position of twirl effect center
- Twirl Axis: Rotation axis (0=Y, 1=X, 2=Z)
- Twirl Falloff: How quickly the effect fades with distance

Rendering Settings:
- Cull Mode: Set to "Off" for two-sided rendering
- Flip Normals: Enable when viewing from inside the funnel
- Alpha: Overall transparency control

Tips for Best Results:
1. For vertical funnels, keep Projection Axis at 0 (Y axis)
2. Start with Texture Scale (1,1) and adjust based on your texture
3. Use Height Scale Factor to compensate for funnel tapering
4. Position Twirl Center at the funnel's center for best effect
5. Enable "Use World Space" if the funnel moves/rotates in the scene

Common Issues & Solutions:
- Texture stretching at top/bottom: Adjust Height Scale Factor
- Twirl too strong/weak: Adjust Twirl Strength and Falloff
- Dark interior: Enable Flip Normals when inside the funnel
- Texture scale wrong: Adjust Texture Scale X/Y values

The shader completely bypasses mesh UVs, so UV mapping quality doesn't matter.