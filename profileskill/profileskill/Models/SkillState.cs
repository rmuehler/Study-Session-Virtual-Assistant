// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;

namespace profileskill.Models
{
    public class SkillState
    {
        public string Token { get; set; }

        public profileskillLuis LuisResult { get; set; }

        public void Clear()
        {
        }
    }
}
