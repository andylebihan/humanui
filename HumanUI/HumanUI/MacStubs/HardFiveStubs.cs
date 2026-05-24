// This file previously hosted Mac placeholder stubs for the Hard 5
// components (3D View, Charts, GraphMapper, GradientEditor, Shape /
// ClickableShapeGrid). All five are now backed by real Eto.Drawable
// implementations under MacStubs/, so this file is intentionally empty
// — kept so the SDK Compile glob still finds something at this path
// and the per-Mac-TFM <Compile Include="MacStubs\**\*.cs"> doesn't
// silently turn up zero files.
//
//   CreateShape_Component / SetShape_Component       → HUI_Shape.cs
//   CreateMultiShape_Component / SetShapes_Component → HUI_MultiShape.cs
//   CreateGraphMapper_Component                      → HUI_GraphMapper.cs
//   CreateGradientEditor_Component                   → HUI_GradientEditor.cs
//   Create/SetChart, Create/SetMultiChart,
//   SetChartAppearance                               → HUI_Charts.cs
//   Create3DView_Component / Set3DView_Component     → HUI_View3D.cs
