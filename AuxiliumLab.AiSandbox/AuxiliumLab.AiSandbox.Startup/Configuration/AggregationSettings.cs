// AggregationSettings and AggregationStep have been moved to the Infrastructure layer
// so that ApplicationServices can reference them without a circular dependency.
// These global aliases preserve backwards compatibility for all code in this project.
global using AggregationSettings = AuxiliumLab.AiSandbox.Infrastructure.Configuration.AggregationSettings;
global using AggregationStep = AuxiliumLab.AiSandbox.Infrastructure.Configuration.AggregationStep;
