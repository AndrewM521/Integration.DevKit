/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.Core;

namespace TestApp.Demos;

/// <summary>
/// Demonstrates <see cref="JsonUtils"/>' dot-path extraction utilities against a JSON file on disk.
/// </summary>
public class CoreClassesDemo : IDemo
{
    public async Task RunAsync()
    {
        OperationResult<string> json;

        //json = await FileUtils.ReadFileTextAsync("C:\\NAS\\Home Drive\\Projects\\Junk\\Json1.txt");
        //if (!json.MethodSuccess)
        //{
        //    throw json.Exception;
        //}

        //json = await FileUtils.ReadFileTextAsync("C:\\NAS\\Home Drive\\Projects\\Junk\\Json2.txt");
        //if (!json.MethodSuccess)
        //{
        //    throw json.Exception;
        //}

        //json = await FileUtils.ReadFileTextAsync("C:\\NAS\\Home Drive\\Projects\\Junk\\Json3.txt");
        //if (!json.MethodSuccess)
        //{
        //    throw json.Exception;
        //}

        json = await FileUtils.ReadFileTextAsync("C:\\NAS\\Home Drive\\Projects\\Junk\\Json4.txt");
        if (!json.MethodSuccess)
        {
            throw json.Exception;
        }

        object result = null;


        //----Get Dictionary by single path----
        //result = JsonUtils.GetDictionary(json.Result).Result; //Root
        //result = JsonUtils.GetDictionary(json.Result, "numbers").Result; //Primitive
        //result = JsonUtils.GetDictionary(json.Result, "data").Result; //Dictionary
        //result = JsonUtils.GetDictionary(json.Result, "house").Result; //Dictionary List
        //result = JsonUtils.GetDictionary(json.Result, "index").Result; //List
        //result = JsonUtils.GetDictionary(json.Result, "data.dictionary1").Result; //Sub Dictionary
        //result = JsonUtils.GetDictionary(json.Result, "data.dictionary1.name").Result; //Sub Dictionary Object
        //result = JsonUtils.GetDictionary(json.Result, "data.activities").Result; //Sub Dictionary List
        //result = JsonUtils.GetDictionary(json.Result, "data.activities.0").Result; //Sub Dictionary List Item
        //result = JsonUtils.GetDictionary(json.Result, "data.activities.0.id").Result; //Sub Dictionary List Item Object

        //----Get List by single path----
        //result = JsonUtils.GetList<int>(json.Result).Result; //Root
        //result = JsonUtils.GetList<int>(json.Result, "testData.luckyNumbers").Result;
        //result = JsonUtils.GetList<double>(json.Result, "testData.prices").Result;
        //result = JsonUtils.GetList<string>(json.Result, "testData.allowedRoles").Result;
        //result = JsonUtils.GetList<int>(json.Result, "testData.matrix.1.2").Result;
        //result = JsonUtils.GetList<int>(json.Result, "testData.emptyList.1").Result;

        //----Get Dictionary List by single path----
        //result = JsonUtils.GetDictionaryList(json.Result).Result; //Root
        //result = JsonUtils.GetDictionaryList(json.Result, "numbers").Result; //Primitive
        //result = JsonUtils.GetDictionaryList(json.Result, "data").Result; //Dictionary
        //result = JsonUtils.GetDictionaryList(json.Result, "house").Result; //Dictionary List
        //result = JsonUtils.GetDictionaryList(json.Result, "index").Result; //List --Should return empty since the objects are not of Dictionary List so we cant convert
        //result = JsonUtils.GetDictionaryList(json.Result, "data.dictionary1").Result; //Sub Dictionary
        //result = JsonUtils.GetDictionaryList(json.Result, "data.dictionary1.name").Result; //Sub Dictionary Object
        //result = JsonUtils.GetDictionaryList(json.Result, "data.activities").Result; //Dictionary List
        //result = JsonUtils.GetDictionaryList(json.Result, "data.activities.0").Result; //Dictionary List Item
        //result = JsonUtils.GetDictionaryList(json.Result, "data.activities.0.id").Result; //Dictionary List Item Object

        //----Get Dictionary by multi-paths----
        //result = JsonUtils.GetDictionary(json.Result, new List<string> { "numbers", "world" }).Result; //Primitives
        //result = JsonUtils.GetDictionary(json.Result, new List<string> { "data", "house" }).Result; //Dictionaries
        //result = JsonUtils.GetDictionary(json.Result, new List<string> { "index", "house" }).Result; //Lists
        //result = JsonUtils.GetDictionary(json.Result, new List<string> { "data.dictionary1", "house.0" }).Result; //Sub Dictionary
        //result = JsonUtils.GetDictionary(json.Result, new List<string> { "data.dictionary1.name", "data.jobs.0" }).Result; //Sub Dictionary Object
        //result = JsonUtils.GetDictionary(json.Result, new List<string> { "data.activities", "data.jobs" }).Result; //Sub Dictionary List
        //result = JsonUtils.GetDictionary(json.Result, new List<string> { "data.activities.0", "data.jobs.0" }).Result; //Sub Dictionary List Item
        //result = JsonUtils.GetDictionary(json.Result, new List<string> { "data.activities.0.id", "data.jobs.0.id" }).Result; //Sub Dictionary List Item Object
        //result = JsonUtils.GetDictionary(json.Result, new List<string> { "data.dictionary1.id", "data.dictionary1.name" }).Result; //Sub Dictionary Same Parent
        //result = JsonUtils.GetDictionary(json.Result, new List<string> { "data.activities.id", "data.activities.name" }).Result; //Sub Dictionary List Same Parent


        //result = JsonUtils.GetDictionary(json.Result, "data.dictionary1", JsonExtractionLayout.PreserveRoot).Result; //Sub Dictionary
        //result = JsonUtils.GetDictionary(json.Result, new List<string> { "data.activities.0.id", "data.jobs.0.id" }).Result; //Sub Dictionary List Item Object

        //result = JsonUtils.GetDictionary(json.Result, new List<string> { "data.empty", "data.dictionary1" }).Result; //Dictionary List
        //result = JsonUtils.GetDictionaryList(json.Result, "data.empty").Result; //Dictionary List
        //result = JsonUtils.GetList<string>(json.Result, "data.empty").Result; //Dictionary List

        //result = JsonUtils.ParseAndFilterJson<int>(json.Result, new List<string> { "data.empty", "data.dictionary1" }).Result; //Dictionary List

        //result = JsonUtils.GetDictionaryList(json.Result, new List<string> { "data.activities.id", "data.activities.name" }).Result; //Sub Dictionary List Same Parent

        result = JsonUtils.GetDictionaryList(json.Result, "data.comments").Result;

        string jsonResult = JsonUtils.SerializeObjectToJson(result).Result;

        Console.WriteLine(jsonResult);
    }
}
