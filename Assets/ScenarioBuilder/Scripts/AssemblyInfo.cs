using System.Runtime.CompilerServices;

// Setup methods of the assets are internal: the inspector is the authoring tool,
// code-side setup is only needed by the tests and the demo content generator.
[assembly: InternalsVisibleTo("ScenarioBuilder.Tests.EditMode")]
[assembly: InternalsVisibleTo("ScenarioBuilder.Demo.Editor")]
