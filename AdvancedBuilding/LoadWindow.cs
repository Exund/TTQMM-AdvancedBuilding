﻿using UnityEngine;
using System.IO;

namespace Exund.AdvancedBuilding
{
    class LoadWindow : MonoBehaviour
    {
        private int ID = 7781;

        private bool visible = false;

        private Vector2 scrollPos = Vector2.zero;

        public Tank loadedTech;

        private string path = "";

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.O) && Input.GetKey(KeyCode.LeftAlt) && Singleton.playerTank)
            {
                visible = true;
            }
        }

        private void OnGUI()
        {
            if (!visible) return;
            if (AdvancedBuildingMod.Nuterra)
            {
                GUI.skin = AdvancedBuildingMod.Nuterra;
            }
            /*GUI.skin = NuterraGUI.Skin;/*.window = new GUIStyle(GUI.skin.window)
            {
                normal =
            {
                background = NuterraGUI.LoadImage("Border_BG.png"),
                textColor = Color.white
            },
                border = new RectOffset(16, 16, 16, 16),
            };*/
            GUI.Window(ID, new Rect((Screen.width - 700f) / 2, (Screen.height - 500f) / 2, 700f, 500f), new GUI.WindowFunction(DoWindow), "Techs");
        }

        private void DoWindow(int id)
        {

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            foreach (var tech in Directory.GetFiles(Path.GetFullPath(AdvancedBuildingMod.PreciseSnapshotsFolder),"*.json"))
            {
                if (GUILayout.Button(tech,new GUIStyle(GUI.skin.button) { richText = true, alignment = TextAnchor.MiddleLeft }))
                {
                    path = tech;
                }
            }
            GUILayout.EndScrollView();
            GUILayout.TextField(path);
            if (GUILayout.Button("Load"))
            {
                try
                {
                    Vector3 position = Singleton.playerTank.trans.position;
                    Quaternion rotation = Singleton.playerTank.trans.rotation;

                    loadedTech = PreciseSnapshot.Load(path, position, rotation);

                    Tank playerTank = Singleton.playerTank;
                    Singleton.Manager<ManTechs>.inst.SetPlayerTank(null, true);
                    playerTank.RemoveFromNetworkedGame();


                    Singleton.Manager<ManTechs>.inst.SetPlayerTank(loadedTech, true);
                }
                catch { }
                visible = false;
            }
            if (GUILayout.Button("Cancel")) visible = false;

        }
    }
}
