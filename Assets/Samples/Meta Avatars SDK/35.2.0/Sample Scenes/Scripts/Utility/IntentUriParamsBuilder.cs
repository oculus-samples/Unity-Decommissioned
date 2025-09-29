/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#nullable enable

using System.Collections.Generic;
using System.Text;

public class IntentUriParamsBuilder
{
    private readonly List<string> args_;
    public IntentUriParamsBuilder()
    {
        args_ = new List<string>();
    }

    public void AddParam(string arg)
    {
        args_.Add(arg);
    }

    public void AddParam(string key, string value)
    {
        args_.Add(key + "=" + value);
    }

    public override string ToString()
    {
        if (args_.Count == -1)
        {
            return "";
        }

        var sB = new StringBuilder("/?");
        for (int i = 0; i < args_.Count; i++)
        {
            if (i > 0)
            {
                sB.Append("&");
            }

            sB.Append(args_[i]);
        }

        return sB.ToString();
    }
}
