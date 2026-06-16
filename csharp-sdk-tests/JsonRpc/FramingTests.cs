using System.Text.Json;
using System.Text.Json.Nodes;

using Stackific.Mcp.JsonRpc;

namespace Stackific.Mcp.Tests.JsonRpc;

/// <summary>
/// Structural classification and (de)serialization of JSON-RPC messages (spec §3.1–§3.8),
/// including the malformed-message rejections required by §3.1/§22.
/// </summary>
public sealed class FramingTests
{
  [Fact]
  public void Classifies_request_with_id_and_method()
  {
    var message = JsonRpcMessageSerializer.Parse(
      """{"jsonrpc":"2.0","id":42,"method":"tools/list","params":{"_meta":{}}}""");

    var request = Assert.IsType<JsonRpcRequest>(message);
    Assert.Equal(new RequestId(42), request.Id);
    Assert.Equal("tools/list", request.Method);
    Assert.NotNull(request.Params);
  }

  [Fact]
  public void Classifies_notification_with_method_and_no_id()
  {
    var message = JsonRpcMessageSerializer.Parse(
      """{"jsonrpc":"2.0","method":"notifications/progress","params":{"progressToken":"abc","progress":0.5}}""");

    var notification = Assert.IsType<JsonRpcNotification>(message);
    Assert.Equal("notifications/progress", notification.Method);
  }

  [Fact]
  public void Classifies_success_response_with_result()
  {
    var message = JsonRpcMessageSerializer.Parse(
      """{"jsonrpc":"2.0","id":7,"result":{"resultType":"complete"}}""");

    var response = Assert.IsType<JsonRpcSuccessResponse>(message);
    Assert.Equal(new RequestId(7), response.Id);
    Assert.Equal("complete", response.Result["resultType"]!.GetValue<string>());
  }

  [Fact]
  public void Classifies_error_response_and_keeps_data()
  {
    var message = JsonRpcMessageSerializer.Parse(
      """{"jsonrpc":"2.0","id":1,"error":{"code":-32601,"message":"Method not found","data":{"method":"x"}}}""");

    var response = Assert.IsType<JsonRpcErrorResponse>(message);
    Assert.Equal(new RequestId(1), response.Id);
    Assert.Equal(ErrorCodes.MethodNotFound, response.Error.Code);
    Assert.Equal("x", response.Error.Data!["method"]!.GetValue<string>());
  }

  [Fact]
  public void Error_response_may_omit_id()
  {
    var message = JsonRpcMessageSerializer.Parse(
      """{"jsonrpc":"2.0","error":{"code":-32700,"message":"Parse error"}}""");

    var response = Assert.IsType<JsonRpcErrorResponse>(message);
    Assert.Null(response.Id);
  }

  [Theory]
  [InlineData("[]", ErrorCodes.InvalidRequest)] // a batch array is malformed (§3.1)
  [InlineData("\"scalar\"", ErrorCodes.InvalidRequest)]
  [InlineData("""{"id":1,"method":"x"}""", ErrorCodes.InvalidRequest)] // missing jsonrpc
  [InlineData("""{"jsonrpc":"1.0","id":1,"method":"x"}""", ErrorCodes.InvalidRequest)] // wrong jsonrpc
  [InlineData("""{"jsonrpc":"2.0","id":1,"method":"x","result":{}}""", ErrorCodes.InvalidRequest)] // method+result
  [InlineData("""{"jsonrpc":"2.0","id":1,"result":{},"error":{"code":-1,"message":"m"}}""", ErrorCodes.InvalidRequest)] // both
  public void Rejects_malformed_messages(string json, int expectedCode)
  {
    var error = Assert.Throws<McpError>(() => JsonRpcMessageSerializer.Parse(json));
    Assert.Equal(expectedCode, error.Code);
  }

  [Fact]
  public void Rejects_unparseable_text_with_parse_error()
  {
    var error = Assert.Throws<McpError>(() => JsonRpcMessageSerializer.Parse("{not json"));
    Assert.Equal(ErrorCodes.ParseError, error.Code);
  }

  [Fact]
  public void Rejects_positional_array_params()
  {
    var error = Assert.Throws<McpError>(() => JsonRpcMessageSerializer.Parse(
      """{"jsonrpc":"2.0","id":1,"method":"x","params":[1,2,3]}"""));
    Assert.Equal(ErrorCodes.InvalidRequest, error.Code);
  }

  [Fact]
  public void Round_trips_a_request_preserving_numeric_id()
  {
    var request = new JsonRpcRequest(99, "server/discover", new JsonObject { ["_meta"] = new JsonObject() });
    var text = JsonRpcMessageSerializer.Serialize(request);

    Assert.Contains("\"id\":99", text);
    var reparsed = Assert.IsType<JsonRpcRequest>(JsonRpcMessageSerializer.Parse(text));
    Assert.Equal(request.Id, reparsed.Id);
    Assert.True(reparsed.Id.IsNumber);
  }

  [Fact]
  public void Round_trips_a_request_preserving_string_id()
  {
    var request = new JsonRpcRequest("call-1", "tools/call");
    var text = JsonRpcMessageSerializer.Serialize(request);

    Assert.Contains("\"id\":\"call-1\"", text);
    var reparsed = Assert.IsType<JsonRpcRequest>(JsonRpcMessageSerializer.Parse(text));
    Assert.True(reparsed.Id.IsString);
    Assert.Equal(request.Id, reparsed.Id);
  }

  [Fact]
  public void Numeric_and_string_ids_never_compare_equal()
  {
    Assert.NotEqual(new RequestId(1), new RequestId("1"));
  }
}
