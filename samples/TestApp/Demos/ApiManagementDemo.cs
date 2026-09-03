/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.Core;
using Integration.DevKit.RESTApiMgmt;
using Integration.DevKit.RESTApiMgmt.Contracts;

namespace TestApp.Demos;

/// <summary>
/// Full CRUD smoke test against a configured <see cref="IApiClient"/>, plus a failure-path request
/// and a metrics dump.
/// </summary>
public class ApiManagementDemo : IDemo
{
    public async Task RunAsync()
    {
        Console.WriteLine("----|----|Api Management|----|----");
        var _apiManager = Service_RESTApiMgmt.ApiManager;
        var _client = _apiManager.GetClient("TestClient");

        _apiManager.LogRuntimeSettings();

        ApiEndpoint Posts = new ApiEndpoint("/Posts");

        Console.WriteLine("GET Posts: ");
        var getAll = await _client.GetAsync(Posts.BuildUrl());
        if (!getAll.MethodSuccess)
        {
            Console.WriteLine(getAll.Exception.Message);
            return;
        }

        Console.WriteLine(getAll.Result);
        Console.WriteLine("----------------------------------------------------");
        Console.WriteLine("GET Post: ");

        var buildUrlGET = Posts.BuildPositionalUrl(new List<object> { 1 });
        if (!buildUrlGET.MethodSuccess)
        {
            Console.WriteLine(buildUrlGET.Exception.Message);
            return;
        }

        var getSingle = await _client.GetAsync(buildUrlGET.Result);
        if (!getSingle.MethodSuccess)
        {
            Console.WriteLine(getSingle.Exception.Message);
            return;
        }

        Console.WriteLine(getSingle.Result);
        Console.WriteLine("----------------------------------------------------");
        Console.WriteLine("POST Post: ");

        var getJsonPOST = JsonUtils.SerializeObjectToJson(new Dictionary<string, object>
        {
            ["title"] = "foo",
            ["body"] = "bar",
            ["userId"] = 1
        });
        if (!getJsonPOST.MethodSuccess)
        {
            Console.WriteLine(getJsonPOST.Exception.Message);
            return;
        }

        var createContentPOST = _client.CreateHttpContent(RESTApiMediaTypes.Json, getJsonPOST.Result);
        if (!createContentPOST.MethodSuccess)
        {
            Console.WriteLine(createContentPOST.Exception.Message);
            return;
        }

        var post = await _client.PostAsync(Posts.BuildUrl(), createContentPOST.Result);
        if (!post.MethodSuccess)
        {
            Console.WriteLine(post.Exception.Message);
            return;
        }

        Console.WriteLine(post.Result);
        Console.WriteLine("----------------------------------------------------");
        Console.WriteLine("PUT Post: ");

        var getJsonPut = JsonUtils.SerializeObjectToJson(new Dictionary<string, object>
        {
            ["id"] = 1,
            ["title"] = "updated title",
            ["body"] = "updated body",
            ["userId"] = 1
        });
        if (!getJsonPut.MethodSuccess)
        {
            Console.WriteLine(getJsonPut.Exception.Message);
            return;
        }

        var createContentPUT = _client.CreateHttpContent(RESTApiMediaTypes.Json, getJsonPut.Result);
        if (!createContentPUT.MethodSuccess)
        {
            Console.WriteLine(createContentPUT.Exception.Message);
            return;
        }

        var buildUrlPUT = Posts.BuildPositionalUrl(new List<object> { 1 });
        if (!buildUrlPUT.MethodSuccess)
        {
            Console.WriteLine(buildUrlPUT.Exception.Message);
            return;
        }

        var put = await _client.PutAsync(buildUrlPUT.Result, createContentPUT.Result);
        if (!put.MethodSuccess)
        {
            Console.WriteLine(put.Exception.Message);
            return;
        }

        Console.WriteLine(put.Result);
        Console.WriteLine("----------------------------------------------------");
        Console.WriteLine("DELETE Post: ");

        var buildUrlDELETE = Posts.BuildPositionalUrl(new List<object> { 1 });
        if (!buildUrlDELETE.MethodSuccess)
        {
            Console.WriteLine(buildUrlDELETE.Exception.Message);
            return;
        }

        var delete = await _client.DeleteAsync(buildUrlDELETE.Result);
        if (!delete.MethodSuccess)
        {
            Console.WriteLine(delete.Exception.Message);
            return;
        }

        Console.WriteLine(delete.Result);
        Console.WriteLine("----------------------------------------------------");
        Console.WriteLine("Failure Test: ");

        var result = await _client.GetAsync("/this-route-does-not-exist");
        if (!result.MethodSuccess)
        {
            Console.WriteLine(result.Exception.Message);
        }

        Console.WriteLine("----------------------------------------------------");
        Console.WriteLine("Client Metrics: ");
        Console.WriteLine(_client.ClientMetrics.ToString());
    }
}
