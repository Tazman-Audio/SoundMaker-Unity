using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Fabric.Player
{

public static class FabricOnlineDocumentation
{
    [MenuItem("Help/SoundMaker/Online Documentation")]
    static void Init()
    {
        Application.OpenURL("https://tazman-audio.screenstepslive.com/m/120400/c/425227");
    }
}

}