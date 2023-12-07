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

## Usage
#### Create a BatchRendererGroupContainer
First, we need to create the **BatchRendererGroupContainer** and set global bounds.
```c#
var bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1048576.0f, 1048576.0f, 1048576.0f));
m_BrgContainer = new BatchRendererGroupContainer(bounds);
```

#### Create a material properties description
For each batch we need to describe material properties (type and property id). A property id of material property can be got by Shader.PropertyToID method.
Please note that material properties description needs only for batch description creating, so we need to dispose it.

P.S.: a batch contains an objectToWorld and a worldToObject matrices by default.
```c#
// for example let's create description of _BaseColor material property.
var materialProperties = new NativeArray<MaterialProperty>(1, Allocator.Temp)
{
     [0] = MaterialProperty.Create<Color>(m_BaseColorPropertyId)
};
```

#### Create a batch description
After that, we need create a batch description that needs max instance count and a material properties description.
```c#
// for example let's create description of _BaseColor material property.
var batchDescription = new BatchDescription(m_CubeCount, materialProperties, Allocator.Persistent);
materialProperties.Dispose(); // dispose the material properties description
```

#### Create a renderer description
Renderer description contains some rendering properties, e.g. ShadowCastingMode, ReceiveShadows or MotionMode property.
```c#
var rendererDescription = new RendererDescription
{
    MotionMode = MotionVectorGenerationMode.Camera,
    ReceiveShadows = true,
    ShadowCastingMode = ShadowCastingMode.On,
    StaticShadowCaster = false,
    RenderingLayerMask = 1,
    Layer = 0
};
```

#### Add the batch to the BRG container
```c#
// After batch adding we get a batch handle.
m_BatchHandle = m_BrgContainer.AddBatch(ref batchDescription, m_Mesh, 0, m_Material, ref rendererDescription);
```
