shader_type canvas_item;
/**
 * Sub-pixel scroll correction, in container-local (screen) pixels.
 * Set every frame by SubViewportContainer.ViewportWholePixelOnlyMovement().
 * Expected range is roughly +/- (0.5 * StretchShrink) on each axis.
 * Leave at zero for strict whole-pixel movement.
 */
uniform vec2 subpixel_offset = vec2(0.0);
/*
 * <summary>
 * Shader vertex stage. Shifts the quad that displays the SubViewport texture
 * by the sub-pixel remainder of the camera, so that a camera snapped to whole
 * game pixels still appears to scroll smoothly at screen-pixel precision.
 * </summary>
 * <param name="VERTEX">Built-in. Container-local vertex position, in screen pixels.</param>
 * <example>
 * Called automatically by the renderer for each vertex of the container's quad.
 * It is not intended to be called directly; drive it via the subpixel_offset uniform.
 * </example>
 */
void vertex() {
	VERTEX += subpixel_offset;
}
// No fragment() is needed: the default samples the SubViewport texture unchanged.