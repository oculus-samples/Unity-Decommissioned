// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Utilities;
using Meta.XR.Samples;

namespace Meta.Decommissioned.Utils
{
    [MetaCodeSample("Decommissioned")]
    public static class NameTruncator
    {
        /**
         * Truncate a name using a given character.
         * <param name="nameString">The name to be truncated.</param>
         * <param name="truncateAtFirstName">If true, the name will be truncated at the first name with no middle/last name initial.</param>
         * <param name="truncateCharacter">Character at which the name will be truncated; this is a space by default.</param>
         */
        public static string TruncateName(string nameString, bool truncateAtFirstName = false, char truncateCharacter = ' ')
        {
            if (nameString.IsNullOrEmpty()) { return ""; }
            var splitStringArray = nameString.Split(truncateCharacter);
            return splitStringArray.Length < 2 || truncateAtFirstName
                ? splitStringArray[0]
                : string.Format("{0} {1}.", splitStringArray[0], splitStringArray[1][0]);
        }
    }
}
