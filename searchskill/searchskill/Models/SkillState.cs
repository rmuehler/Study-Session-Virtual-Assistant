// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;

namespace searchskill.Models
{
    public class SkillState
    {
        public string Token { get; set; }

        public searchskillLuis LuisResult { get; set; }

        public void Clear()
        {
        }
    }
}
