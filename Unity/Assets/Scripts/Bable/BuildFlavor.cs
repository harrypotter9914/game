namespace Bable {
 /// <summary>Both players use the same gameplay assemblies and assets. Only entry points differ.</summary>
 public static class BuildFlavor {
  public static bool Practice {
   get {
#if BABLE_PRACTICE
    return true;
#elif UNITY_EDITOR
    return UnityEditor.SessionState.GetBool("Bable.PracticePreview",false);
#else
    return false;
#endif
   }
  }
  public static bool CanOpenPractice {
   get {
#if UNITY_EDITOR
    return true; // Editor regression tests can exercise both scene sets.
#else
    return Practice;
#endif
   }
  }
  public static string StorageFolder => Practice ? "BabelPractice" : "Babel";
  public static bool AllowsScene(string scene) {
#if UNITY_EDITOR
   return true;
#else
   return scene=="MainMenu" || (Practice
    ? scene=="Rune_Combat_Lab" || scene.StartsWith("Boss_Test_")
    : scene=="Gameplay_Main");
#endif
  }
 }
}
