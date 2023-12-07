## About
What is **Batch Rendering Group** (BRG)? BatchRendererGroup is an API for high-performance custom rendering in projects that use a Scriptable Render Pipeline (SRP) and the SRP Batcher. 
BRG is the perfect tool to:
 - Render DOTS Entities. For example, Unity’s Hybrid Renderer uses BRG to do this.
 - Render a large number of environment objects where using individual GameObjects would be too resource-intensive. For example, procedurally-placed plants or rocks.
 - Render custom terrain patches. You can use different meshes or materials to display different levels of detail.

More information about BRG - https://docs.unity3d.com/Manual/batch-renderer-group.html.

The Unity **Batch Rendering Group Tool** that provides a high-level API for instancing data reading and writing. It supports UBO and SSBO buffer types, so it can be used for GLES. Current version of tool has only **Frustum culling**.

## Dependencies
 - Unity Mathematics: 1.2.6
 - Unity Collections: 2.2.1
 - Unity Burst: 1.8.8
