using UnityEngine;

/// <summary>
/// STAGEBUTTONINSTANCE - Stage selection button data.
/// 
/// PURPOSE:
/// Holds data for a stage selection button in the stage
/// select screen, primarily the stage identifier.
/// 
/// PROPERTIES:
/// - stageName: Identifier linking to StageLibrary entry
/// 
/// RELATED FILES:
/// - StageSelectManager.cs: Creates and manages buttons
/// - StageLibrary.cs: Stage definitions
/// - ScreenWidthButtonFactory.cs: Button creation
/// </summary>
public class StageButtonInstance : MonoBehaviour
{
    [SerializeField] public string stageName;  
}
