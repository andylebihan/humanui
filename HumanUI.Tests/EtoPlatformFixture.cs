// Intentionally empty. An earlier draft of these tests tried to
// initialize an Eto Platform handler in-process so per-control
// round-trip tests could exercise actual Eto.Forms.Slider /
// TextBox / etc. — that turned out to be impractical because the
// NuGet Eto.Platform.Wpf has a different PublicKeyToken than the
// McNeel-signed Eto.dll the project compiles against (the same
// reason UiIntegrationTests.MainWindow_ConstructsWithoutException
// is [Fact(Skip = ...)]).
//
// File kept as a marker so this design decision is searchable from
// the test tree; delete once we adopt a different approach (e.g. a
// Rhino-hosted integration runner that provides a real platform).
