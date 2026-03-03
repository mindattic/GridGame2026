using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;


namespace Scripts.Managers
{

  

    public class LogManager : MonoBehaviour
    {
        private string log;
        private List<string> messages = new List<string>();
        public LogLevel logLevel = LogLevel.None;
        const int MaxMessages = 10;


        public string text
        {
            get => log;
            set => log = value;
        }


        /// <summary>Info.</summary>
        public void Info(string message)
        {
            if (logLevel < LogLevel.Info)
                return;

            Debug.Log(message);
            messages.Add($@"<color=""white"">{message}</color>");
        }

        /// <summary>Success.</summary>
        public void Success(string message)
        {
            if (logLevel < LogLevel.Success)
                return;

            Debug.Log(message);
            messages.Add($@"<color=""green"">{message}</color>");
        }

        /// <summary>Warning.</summary>
        public void Warning(string message)
        {
            if (logLevel < LogLevel.Warning)
                return;

            Debug.LogWarning(message);
            messages.Add($@"<color=""orange"">{message}</color>");
        }


        /// <summary>Error.</summary>
        public void Error(string message)
        {
            if (logLevel < LogLevel.Error)
                return;

            Debug.LogError(message);
            messages.Add($@"<color=""red"">{message}</color>");
        }


        /// <summary>Exception.</summary>
        public void Exception(UnityException ex)
        {
            if (logLevel < LogLevel.Error)
                return;

            Debug.LogError(ex.Message.ToString());
            messages.Add($@"<color=""red"">{ex.Message.ToString()}</color>");
        }

        /// <summary>Runs per-frame update logic.</summary>
        private void Update()
        {
            if (logLevel == LogLevel.None)
                return;

            if (messages.Count > MaxMessages)
                messages.RemoveAt(0);

            //Print in descending order
            log = string.Join(Environment.NewLine, messages.OrderByDescending(x => x.ToString()));
        }



    }
}
