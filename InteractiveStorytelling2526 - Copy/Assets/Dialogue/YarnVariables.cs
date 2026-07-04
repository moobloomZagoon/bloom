using Yarn.Unity;

[System.CodeDom.Compiler.GeneratedCode("YarnSpinner", "3.2.4.0")]
public partial class YarnVariables : Yarn.Unity.InMemoryVariableStorage, Yarn.Unity.IGeneratedVariableStorage {
    // Accessor for Number $has_matches
    public float HasMatches {
        get => this.GetValueOrDefault<float>("$has_matches");
        set => this.SetValue<float>("$has_matches", value);
    }

    // Accessor for Number $has_blade
    public float HasBlade {
        get => this.GetValueOrDefault<float>("$has_blade");
        set => this.SetValue<float>("$has_blade", value);
    }

}
