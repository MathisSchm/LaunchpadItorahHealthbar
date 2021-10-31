using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using GrimbartTales;
using GrimbartTales.Base;
using GrimbartTales.Platformer2D.CharacterController;
using GrimbartTales.Platformer2D.DamageSystem;
using System.Linq;
using Midi;

namespace ItorahPlugin
{
    [BepInPlugin("org.bepinex.plugins.Testttt", "ItorahMod", "0.1.0.0")]
    public class ItorahMod : BaseUnityPlugin
    {
        private Harmony harmony;

        internal void Awake()
        {
            this.harmony = new Harmony("com.mat.ItorahMod");
        }
        public Midi.OutputDevice midiOutput;
        public void Start()
        {
            MethodInfo methodInfo = AccessTools.Method(typeof(LifePoints), "Update", null, null);
            MethodInfo method = typeof(LifePatch).GetMethod("LifePatchMethod");
            harmony.Patch(methodInfo, new HarmonyMethod(method), null, null, null, null);

            
            MethodInfo methodInfo2 = AccessTools.Method(typeof(HealCharges), "TryHeal", null, null);
            MethodInfo method2 = typeof(ChargePatch).GetMethod("ChargePatchMethod");
            harmony.Patch(methodInfo2, new HarmonyMethod(method2), null, null, null, null);


            midiOutput = OutputDevice.InstalledDevices[1];
            midiOutput.Open();
            //Turn LEDs off. 
            midiOutput.SendSysEx(new byte[] { 240, 0, 32, 41, 2, 24, 14, 0, 247 });
        }

        public void Light(int led, byte red, byte green, byte blue)
        {

            // 3 rows. 
            midiOutput.SendSysEx(new byte[] { 240, 0, 32, 41, 2, 24, 13, 5, 0, 247 });
            midiOutput.SendSysEx(new byte[] { 240, 0, 32, 41, 2, 24, 13, 6, 0, 247 });
            midiOutput.SendSysEx(new byte[] { 240, 0, 32, 41, 2, 24, 13, 7, 0, 247 });

            for (int i = 61; i < led + 61; i++)
            {
                byte ff = Convert.ToByte(i);
                midiOutput.SendSysEx(new byte[] { 240, 0, 32, 41, 2, 24, 11, ff, red, green, blue, 247 });
            }
            for (int i = 71; i < led + 71; i++)
            {
                byte ff = Convert.ToByte(i);
                midiOutput.SendSysEx(new byte[] { 240, 0, 32, 41, 2, 24, 11, ff, red, green, blue, 247 });
            }
            for (int i = 81; i < led + 81; i++)
            {
                byte ff = Convert.ToByte(i);
                midiOutput.SendSysEx(new byte[] { 240, 0, 32, 41, 2, 24, 11, ff, red, green, blue, 247 });
            }
        
        }

        int lasthealth = 0;
        int healthCurrent = 20;
        int maxHealth = 20;

        public void Charge(int charges)
        {
            midiOutput.SendSysEx(new byte[] { 240, 0, 32, 41, 2, 24, 13, 4, 0, 247 });
            for (int i = 0; i < charges; i++)
            {
                byte ff = Convert.ToByte(52 + (i*2));
                midiOutput.SendSysEx(new byte[] { 240, 0, 32, 41, 2, 24, 11, ff, 30, 0, 60, 247 });
            }
            healthCurrent = maxHealth;
        }

        public void Check(int health, int keyTotal)
        {
            byte red = Convert.ToByte(Convert.ToInt32(Mathf.Clamp((float)(63) - ((float)healthCurrent / (float)maxHealth) * (float)63, 0, 63)));
            byte green = Convert.ToByte(Convert.ToInt32(Mathf.Clamp(((float)healthCurrent / (float)maxHealth) * (float)63, 0, 63)));
            byte blue = 0;
            healthCurrent = health; 
            if (health != lasthealth)
            {
                Light(keyTotal, red, green, blue);
            }
            if(health < lasthealth)
            {
                // Flash red. 
                Light(keyTotal, Convert.ToByte(63), blue, blue);
                System.Threading.Thread.Sleep(100);
                Light(keyTotal, red, green, blue);
            }
            lasthealth = health;
        }
    }

    [HarmonyPatch(typeof(LifePoints))]
    public class LifePatch
    {


        // Token: 0x0600000D RID: 13 RVA: 0x000024F4 File Offset: 0x000006F4
        [HarmonyPrefix]
        public static void LifePatchMethod(LifePoints __instance)
        {
            
            if (__instance.gameObject.name.ToString() == "itorah")
            {
                BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                FieldInfo field = typeof(LifePoints).GetField("CurrentPoints", bindingAttr);
                FieldInfo field2 = typeof(LifePoints).GetField("Maximum", bindingAttr);
                int health = Convert.ToInt32(field.GetValue(__instance)) + 1;
                int maxHealth = Convert.ToInt32(field2.GetValue(__instance));
                int launchpadKeys = 8;
                float intervals = (float)maxHealth / (float)launchpadKeys;
                int finalKeys = Mathf.RoundToInt((float)health / intervals);
                Transform.FindObjectOfType<ItorahMod>().Check(health, finalKeys);
            }
        }
    }


    [HarmonyPatch(typeof(HealCharges))]
    public class ChargePatch
    {
        [HarmonyPrefix]
        public static void ChargePatchMethod(HealCharges __instance)
        {
                BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                FieldInfo field = typeof(HealCharges).GetField("CurrentCharges", bindingAttr);
                int charges = Convert.ToInt32(field.GetValue(__instance));
                Transform.FindObjectOfType<ItorahMod>().Charge(Mathf.Clamp(charges - 1, 0, 3));
        }
    }

}

