using GTA;
using GTA.Native;
using System;

namespace InworldV.Helper
{
    internal static class SceneHelper
    {

        /*
        * "CLEAR"
           "EXTRASUNNY"
           "CLOUDS"
           "OVERCAST"
           "RAIN"
           "CLEARING"
           "THUNDER"
           "SMOG"
           "FOGGY"
           "XMAS"
           "SNOWLIGHT"
           "BLIZZARD"
       */
        public static void SetTime(int val, string weather = "CLEAR")
        {
            Enum.TryParse<Weather>(weather, out Weather weatherActual);
            World.Weather = weatherActual;
            Function.Call(Hash.SET_CLOCK_TIME, val, 0, 0);
        }


        public static void SetupCompanionPed(GTA.Ped Character, int type = 1)
        {
            if (type == 1)
            {
                // female
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, Character, 0, 2, 0, 0);
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, Character, 3, 1, 0, 0);
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, Character, 9, 1, 0, 0);
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, Character, 10, -1, 0, 0);
                Function.Call(Hash.SET_PED_PROP_INDEX, Character, 0, 0, 0, true);
            }
            else if (type == 2)
            {
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, Character, 0, 0, 0, 0);
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, Character, 3, 1, 0, 0);
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, Character, 9, 1, 0, 0);
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, Character, 10, 0, 0, 0);
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, Character, 11, 0, 0, 0);
            }
            else if (type == 3)
            {
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, Character, 0, 2, 0, 0);
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, Character, 3, 1, 0, 0);
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, Character, 9, 1, 0, 0);
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, Character, 10, -1, 0, 0);
            }
        }
    }
}
