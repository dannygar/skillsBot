﻿using System.Collections.Generic;
using System;
using System.Linq;
using TravelAgentBot.DialogRootBot;

namespace TravelAgentBot.Authentication
{
    public static class AllowedCallersHelper
    {
        public static IList<string> AllowedCallers(SkillsConfiguration skillsConfig)
        {
            if (skillsConfig == null)
            {
                throw new ArgumentNullException(nameof(skillsConfig));
            }

            // Load the appIds for the configured skills (we will only allow responses from skills we have configured).
            return (from skill in skillsConfig.Skills.Values select skill.AppId).ToList();
        }
    }
}
