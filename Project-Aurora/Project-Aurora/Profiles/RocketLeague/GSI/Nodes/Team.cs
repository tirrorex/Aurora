﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurora.Profiles.RocketLeague.GSI.Nodes
{
    public class Team_RocketLeague : Node<Team_RocketLeague>
    {
        /// <summary>
        /// Name of the team. Usually Blue or Orange, but can be different in custom games and for clan teams
        /// </summary>
        public string Name;

        /// <summary>
        /// Number of goals the team scored
        /// </summary>
        public int Goals;

        /// <summary>
        /// Red value of the teams color (0-1)
        /// </summary>
        public float Red;

        /// <summary>
        /// Green value of the teams color (0-1)
        /// </summary>
        public float Green;

        /// <summary>
        /// Blue value of the teams color (0-1)
        /// </summary>
        public float Blue;

        internal Team_RocketLeague(string JSON) : base(JSON)
        {
            Name = GetString("name");
            Goals = GetInt("goals");
            Red = GetFloat("red");
            Green = GetFloat("green");
            Blue = GetFloat("blue");
        }

        public Color TeamColor => Color.FromArgb((int)(Red * 255.0f),
                                                (int)(Green * 255.0f),
                                                (int)(Blue * 255.0f));
    }
}