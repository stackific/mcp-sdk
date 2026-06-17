# Model Context Protocol — Specification

The Model Context Protocol (MCP) is an open, language-agnostic wire protocol that standardizes how host applications embedding language models obtain context from, and invoke capabilities provided by, external programs over JSON-RPC 2.0 carried as UTF-8 JSON messages. It defines three roles — host, client, and server — exchanging requests, responses, and notifications; a stateless per-request model in which every client request carries its own protocol revision, client identity, and client capabilities inside a reserved `_meta` envelope; a discovery exchange (`server/discover`) through which servers advertise supported protocol revisions and capabilities; server features (tools, resources, prompts, completion) and client features (elicitation); cross-cutting utilities (progress, cancellation, pagination, logging, trace context); transports (stdio and Streamable HTTP); an authorization framework for HTTP transports; and a capability-gated extension mechanism. This document is the complete and authoritative definition of that wire contract: it specifies every message, field, value, error condition, and behavior required to implement an interoperable MCP client or server SDK in any programming language using only this document. The protocol revision defined here carries the wire value `"2026-07-28"` [MCP].

## Status of This Document

This document is a normative specification. The requirement keywords used throughout (MUST, MUST NOT, REQUIRED, SHALL, SHALL NOT, SHOULD, SHOULD NOT, RECOMMENDED, MAY, OPTIONAL) carry the meanings defined in §2 Conformance, Terminology, and Notation [RFC2119][RFC8174]. Citation markers in square brackets (for example [MCP], [JSONRPC2], [SEP-2575]) are provenance only: they attribute source material and never carry normative force. Every normative rule, type, value, and behavior is stated in full inline, so that the document is implementable without consulting any cited work. Citation markers resolve to full references in §30 Normative and Informative References, compiled when the document is complete.

## Table of Contents

- **PART I — FOUNDATIONS**
  - §1 Introduction
  - §2 Conformance, Terminology, and Notation
  - §3 Base Message Format
  - §4 Request Metadata and the Stateless Model
  - §5 Protocol Revision, Version Negotiation, and Discovery
  - §6 Capabilities and Extensions
- **PART II — TRANSPORTS**
  - §7 Transport Model
  - §8 The stdio Transport
  - §9 The Streamable HTTP Transport
  - §10 Server-to-Client Streaming and Subscriptions
- **PART III — INTERACTION PATTERNS AND COMMON STRUCTURES**
  - §11 Multi-Round-Trip Requests
  - §12 Pagination
  - §13 Response Caching
  - §14 Common Data Types
  - §15 Utilities: Progress, Cancellation, Logging, and Trace Context
- **PART IV — SERVER FEATURES**
  - §16 Tools
  - §17 Resources
  - §18 Prompts
  - §19 Completion
- **PART V — CLIENT FEATURES**
  - §20 Elicitation
  - §21 Deprecated Client-Provided Capabilities
- **PART VI — ERRORS AND AUTHORIZATION**
  - §22 Error Handling and Error Codes
  - §23 Authorization
- **PART VII — EXTENSIONS**
  - §24 The Extension Mechanism
  - §25 The Tasks Extension
  - §26 The Interactive User-Interface Extension
- **PART VIII — GOVERNANCE, SECURITY, AND REFERENCE**
  - §27 Feature Lifecycle and Deprecation
  - §28 Security Considerations
  - §29 Conformance Requirements
  - §30 Normative and Informative References
  - Appendix A. Method and Notification Index
  - Appendix B. Error Code Registry
  - Appendix C. Reserved _meta Key Registry
  - Appendix D. Capability Registry
  - Appendix E. Consolidated Type Index

# Part I — Foundations

## §1 Introduction

The Model Context Protocol (MCP) is an open protocol that standardizes how host applications, which embed language models, obtain context from and invoke capabilities provided by external programs. A host application that wishes to give its model access to files, databases, search indexes, business systems, or any other tool or data source would otherwise need a bespoke integration for each such source. MCP solves this by defining a single, uniform wire contract: any program that speaks MCP as a server can supply context and capabilities to any program that speaks MCP as a client, with no integration code specific to the pairing. The protocol is focused on context exchange and the coordination of model-assisted operations between clients and servers, carried over structured JSON messages [MCP][JSONRPC2].

This document is the complete and authoritative definition of that wire contract. It specifies every message, field, value, error condition, and behavior required to implement an MCP client or MCP server that interoperates with any conforming counterpart. The protocol revision defined here carries the wire value `"2026-07-28"`.

### §1.1 Roles

MCP defines three roles. An implementation acts in one or more of these roles.

- **Host** — the application that embeds one or more language models and owns the relationship with the user. The host creates and manages client connectors, controls which connections may be established and their lifecycle, enforces security policy and consent requirements, makes user authorization decisions, coordinates integration with its language model, and aggregates context drawn from multiple sources. The host is the trust boundary: it decides what a model may see and what an external program may do on the user's behalf [MCP].

- **Client** — a connector created and run inside the host. Each client maintains a connection to exactly one server, attaches the protocol revision and the client's capabilities to every request it sends, routes protocol messages in both directions between the host and that server, manages subscriptions and incoming notifications, and preserves the isolation boundary around that one server so that one server cannot observe another [MCP].

- **Server** — a program that exposes context and capabilities through MCP. A server operates independently with a focused responsibility, exposes its features to clients, may request input from the client while servicing a request, and may run as a local process or as a remote service [MCP].

The relationship among the roles is fixed. A single host MAY run many clients simultaneously. Each client has a one-to-one relationship with a single server: a client connects to exactly one server, and the host coordinates across its several clients. Servers do not communicate with one another and do not see the full conversation held by the host; each server receives only the contextual information the host chooses to provide [MCP].

```
  Host process
  ┌───────────────────────────┐
  │ Host                      │        ┌──────────┐
  │  ├─ Client 1 ─────────────┼───────▶│ Server A │
  │  ├─ Client 2 ─────────────┼───────▶│ Server B │
  │  └─ Client 3 ─────────────┼───────▶│ Server C │
  └───────────────────────────┘        └──────────┘
```

### §1.2 Communication Model

Clients and servers exchange structured JSON messages that conform to JSON-RPC 2.0 [JSONRPC2][RFC8259]. Three message kinds are used: **requests**, which carry an `id` and expect a single reply; **responses**, which reply to a request with either a result or an error and echo the request `id`; and **notifications**, which are one-way messages carrying no `id` and expecting no reply. The exact envelope, field names, and constraints for each kind are defined in §3 Base Message Format.

Messages flow in both directions. The predominant direction is client-to-server: the client issues requests and the server replies. The protocol also allows a server to obtain input from the client while it is still servicing a client request, by returning a reply that signals more input is required rather than a final result; the client supplies the requested input and the original operation continues. This multi-round-trip pattern is the mechanism by which servers reach back to clients, and it is defined in §11 Multi-Round-Trip Requests [SEP-2322][SEP-2260]. Beyond this, a server streams notifications to a client only within the lifetime of a long-lived request the client has opened for that purpose; see §10 Server-to-Client Streaming and Subscriptions.

Conceptually, servers expose the larger set of features **to** clients, and clients expose a small set of features **to** servers. Which features either party may use in a given exchange is governed by capabilities. MCP performs capability negotiation per request rather than once per connection: each client request carries the client's declared capabilities, and a server advertises its own capabilities in reply to a discovery request the client MAY issue before any other request. Neither party may rely on a capability the other has not declared. Capability fields and negotiation rules are defined in §6 Capabilities and Extensions [SEP-2575].

### §1.3 Feature Overview

The protocol defines the following features. Each is named here by its purpose and the wire method or notification family that identifies it; the full definition of each appears in the referenced section.

Server features (exposed by a server to a client):

- **Tools** — model-invocable operations the server makes available, listed via `tools/list` and invoked via `tools/call`. See §16 Tools.
- **Resources** — addressable pieces of context (such as files or records) the server can supply, listed via `resources/list` and retrieved via `resources/read`, with update streams available through subscriptions. See §17 Resources.
- **Prompts** — reusable, parameterized prompt or message templates the server offers, listed via `prompts/list` and instantiated via `prompts/get`. See §18 Prompts.
- **Completion** — argument autocompletion suggestions for prompt arguments and resource template variables, requested via `completion/complete`. See §19 Completion.

Client features (exposed by a client to a server):

- **Elicitation** — a structured request, made by the server to the client via `elicitation/create`, asking the host to obtain specific input from the user. See §20 Elicitation.

Utilities are cross-cutting facilities, including connectivity checks, request cancellation, progress reporting, pagination of list results, logging, trace context, and argument completion; these are defined in §15 Utilities: Progress, Cancellation, Logging, and Trace Context. Capability discovery is performed via `server/discover`; see §5 Protocol Revision, Version Negotiation, and Discovery.

The protocol also supports optional **extensions**: negotiated additions that introduce further methods, result types, or behaviors beyond the core feature set without altering it. Extensions are defined in §24 The Extension Mechanism [SEP-2133]. Certain capabilities present in the protocol are marked with the status label **Deprecated** and SHOULD NOT be relied upon by new implementations; such features are flagged at their point of definition [SEP-2577][SEP-2596].

### §1.4 Scope and How to Read This Document

This document defines the wire protocol in full: the message envelope, the lifecycle and capability model, the per-request metadata every message carries, the transports over which messages travel, the authorization framework for HTTP-based transports, every server and client feature, the utilities, and the extension mechanism. An engineer can implement a fully interoperable client or server SDK in any programming language using only this document. The document is language-agnostic: it describes behavior in terms of clients, servers, senders, receivers, and hosts, and expresses every JSON wire shape in the type notation introduced at the head of the document. It does not prescribe any particular programming-language API, class, or library.

The document is organized into parts that move from foundation to feature. Foundational material — base message format, the stateless per-request model, protocol revision and discovery, capability negotiation, and transports — comes first and MUST be implemented by every conforming party. Interaction patterns and the authorization framework follow. The server features (Tools, Resources, Prompts, Completion), the client feature (Elicitation), the utilities, and the extension mechanism come last and MAY be implemented selectively according to the declared capabilities of an implementation. Sections cross-reference one another by section number and title.

Throughout this document the key words **MUST**, **MUST NOT**, **REQUIRED**, **SHALL**, **SHALL NOT**, **SHOULD**, **SHOULD NOT**, **RECOMMENDED**, **MAY**, and **OPTIONAL** are to be interpreted as described in [RFC2119] and [RFC8174]: they have the defined meanings only when, and exactly as, they appear in upper case.

### §1.5 Design Principles

The following principles inform the protocol and explain the shape of the rules that follow.

- **Statelessness and self-contained requests.** MCP is a stateless protocol: all information needed to process a request is contained within that request. A server processes each request independently and MUST NOT infer state from prior requests on the same connection or stream — including the protocol revision, the client's identity, and the client's capabilities, each of which is carried in the request's `_meta` field on every request. A server MUST NOT treat the identity of a connection or process (for example, a long-lived standard-I/O process) as a proxy for a conversation, thread, or session; clients MAY interleave unrelated requests over the same transport. State that must span multiple requests MUST be referenced by an explicit identifier the client supplies on each request. The exact per-request metadata fields are defined in §4 Request Metadata and the Stateless Model [SEP-2575][SEP-2567].

- **Explicit user consent and host-mediated trust.** The host is the trust boundary. It enforces security policy, obtains user authorization decisions, and controls what context is shared with each server and what actions a server may take on the user's behalf. A server receives only the contextual information the host provides and cannot read the host's full conversation or observe other servers; the host enforces this isolation [MCP].

- **Per-request capability negotiation.** Clients and servers declare the features they support, and both parties MUST respect the other's declared capabilities throughout an interaction. Declaration occurs per request: a client attaches its capabilities to each request it sends, and a server advertises its capabilities in reply to discovery. A party MUST NOT exercise a feature the other party has not declared support for. The capability types and the discovery exchange are defined in §6 Capabilities and Extensions and §5 Protocol Revision, Version Negotiation, and Discovery [SEP-2575].

- **Extensibility.** The core protocol provides a minimal required surface, and additional functionality is introduced through negotiated extensions rather than by changing the core. Servers and clients can therefore evolve independently while remaining interoperable, advertising and consuming only the capabilities they support. The extension framework is defined in §24 The Extension Mechanism [SEP-2133].

## §2 Conformance, Terminology, and Notation

This section establishes the vocabulary, requirement keywords, JSON value model, type notation, numeric handling, and document-wide naming rules used throughout this specification. Every other section presumes the definitions and rules stated here. The literal string `2026-07-28` is the wire value of the protocol revision; it appears only as the value of the protocol-version field [MCP].

### §2.1 Conformance and Requirement Keywords

The key words **MUST**, **MUST NOT**, **REQUIRED**, **SHALL**, **SHALL NOT**, **SHOULD**, **SHOULD NOT**, **RECOMMENDED**, **MAY**, and **OPTIONAL** in this document are to be interpreted as described below, and carry these meanings only when they appear in all uppercase letters [RFC2119][RFC8174]:

- **MUST**, **REQUIRED**, **SHALL** — an absolute requirement of the specification. A conforming implementation does this without exception.
- **MUST NOT**, **SHALL NOT** — an absolute prohibition of the specification. A conforming implementation never does this.
- **SHOULD**, **RECOMMENDED** — the behavior is expected in the general case; an implementation MAY deviate only when the full implications are understood and weighed, and a valid reason exists.
- **SHOULD NOT**, **NOT RECOMMENDED** — the behavior is to be avoided in the general case; an implementation MAY adopt it only when the full implications are understood and a valid reason exists.
- **MAY**, **OPTIONAL** — the behavior is truly discretionary. An implementation that includes it and one that omits it are both conforming, and each MUST interoperate with the other (possibly with reduced functionality) without either treating the other's choice as an error.

The same words in lowercase or mixed case carry their ordinary English meaning and impose no conformance requirement [RFC8174]. Where this document states a requirement on a "sender" or "receiver", it binds whichever role (client or server, see §2.2 Core Terminology) is acting in that capacity for the message in question.

An implementation is **conforming** if and only if it satisfies every applicable **MUST**/**MUST NOT**/**SHALL**/**SHALL NOT** requirement of this document for the roles and features it implements. All implementations MUST support the base message format, protocol revision handling, and the core message patterns; any other feature MAY be omitted, but if implemented MUST conform to the rules defined for it [MCP].

A requirement that applies only when a particular feature, capability, or extension is present is conditional on that feature; an implementation that does not offer the feature is not bound by requirements scoped to it, except where this document states otherwise.

### §2.2 Core Terminology

The following terms are used with the precise meanings given here throughout this document [MCP].

- **Host** — the language-model application that embeds and coordinates one or more clients in order to make external tools, data, and capabilities available to a language model. The host owns user trust and consent decisions and is the ultimate consumer of server functionality. The host is not itself a JSON-RPC role on the wire.
- **Client** — a protocol endpoint, typically operated within a host, that initiates connections to servers and issues requests to obtain tools, resources, prompts, and other capabilities. A client is a sender of client-originated requests and a receiver of their responses, and it MAY also receive server-embedded input requests (delivered inside results; see §11 Multi-Round-Trip Requests) and notifications.
- **Server** — a protocol endpoint that exposes tools, resources, prompts, and other capabilities and that responds to client requests. A server is a receiver of client-originated requests and a sender of their responses, and it MAY also send server-embedded input requests (delivered inside results; see §11 Multi-Round-Trip Requests) and notifications.
- **Peer** — the endpoint at the opposite end of a connection from a given endpoint. From a client's standpoint the peer is a server, and vice versa.
- **Sender** — the endpoint that transmits a given message.
- **Receiver** — the endpoint that the given message is addressed to and that processes it.
- **Implementation** — a concrete piece of software acting as a client or server, identified on the wire by a name and a version (see §2.2.1 The Implementation Descriptor).
- **Request** — a message that initiates an operation and that requires the receiver to return exactly one matching response. A request carries an identifier (`id`), a `method` naming the operation, and OPTIONAL parameters.
- **Response** — a message returned in reply to a request, correlated to it by identifier. A response is either a result or an error, never both.
- **Result** — the payload of a successful response, conveying the outcome of the requested operation.
- **Error** — the payload of an unsuccessful response, conveying a numeric `code`, a human-readable `message`, and OPTIONAL structured `data`.
- **Notification** — a one-way message that initiates no operation requiring a reply. A notification carries a `method` and OPTIONAL parameters but no identifier, and the receiver MUST NOT send any response to it.
- **Capability** — a declared unit of optional functionality that an endpoint supports for a given request (see §2.2.2 Capabilities). Capabilities govern which optional features a peer MAY rely upon.
- **Extension** — a self-contained, separately identified body of additional functionality layered on the base protocol, negotiated through capabilities, that MAY add request methods, notification methods, result types, error codes, or reserved metadata keys (see §2.6 Reserved Names, Extensions, and Forward Compatibility) [SEP-2133].
- **Feature** — any individually addressable unit of protocol functionality (such as a single request method, notification, or capability), whether part of the base protocol or contributed by an extension.
- **Deprecated** — the status label applied to a feature that remains defined and that conforming implementations MUST continue to accept, but whose use is discouraged. A feature marked **Deprecated** SHOULD NOT be relied upon by new implementations; receivers MUST still process it according to its definition while it bears this status [SEP-2596].

#### §2.2.1 The Implementation Descriptor

An implementation identifies itself on the wire with an `Implementation` object. It carries at minimum a name and version; additional descriptive fields are OPTIONAL. The full shape is defined in §14 Common Data Types and is reproduced where it is first used in §4 Request Metadata and the Stateless Model [MCP].

```ts
interface Implementation {
  name: string;        // REQUIRED. Programmatic identifier of the software.
  version: string;     // REQUIRED. Version string of the software.
  title?: string;      // OPTIONAL. Human-readable display name.
  icons?: Icon[];      // OPTIONAL. Visual identifiers (see §14 Common Data Types).
  // ... plus description?, websiteUrl? — see §14.3 for the full shape.
  // Additional implementation-defined properties MAY appear and MUST be ignored
  // by receivers that do not recognize them (see §2.3.4).
}
```

#### §2.2.2 Capabilities

A **capability** is a discrete optional feature an endpoint declares support for. Capabilities are declared per request rather than once for the lifetime of a connection: a client states the capabilities relevant to each request, and a server MUST NOT infer a capability from any prior request, connection, or stream [SEP-2575][SEP-2567]. An endpoint MUST NOT rely on a capability the peer has not declared for the request in question; if processing a request requires a capability the client did not declare, the server MUST reject the request with the dedicated missing-capability error (see §5 Protocol Revision, Version Negotiation, and Discovery, §6 Capabilities and Extensions, and §22 Error Handling and Error Codes). The concrete `ClientCapabilities` and `ServerCapabilities` shapes are defined in §6 Capabilities and Extensions.

### §2.3 JSON Value Model

All messages defined by this protocol are JSON documents and MUST be valid JSON [RFC8259][JSONRPC2]. This document uses a single value model, named here `JSONValue`, for every value that crosses the wire [RFC8259].

```ts
type JSONValue =
  | string
  | number
  | boolean
  | null
  | JSONObject
  | JSONArray;

type JSONObject = { [key: string]: JSONValue };  // unordered, string-keyed map
type JSONArray  = JSONValue[];                    // ordered list
```

#### §2.3.1 Object and Array Semantics

- A JSON **object** is an unordered collection of name/value pairs (members) whose names are strings. Within a single object, names SHOULD be unique; a receiver encountering a duplicate name within one object MAY treat the document as malformed, and if it does not, it MUST behave as though only the last occurrence of that name is present. Senders MUST NOT emit duplicate names within one object [RFC8259].
- Member ordering carries no meaning. Receivers MUST NOT depend on the textual order of object members, and senders MUST NOT assume any order is preserved.
- A JSON **array** is an ordered sequence of values. Array order is significant and MUST be preserved by senders and respected by receivers.

#### §2.3.2 Encoding

All JSON text exchanged by this protocol MUST be encoded as UTF-8 [RFC8259]. Receivers MUST accept any well-formed UTF-8 JSON document; senders MUST NOT emit any other character encoding. Where a transport frames or wraps JSON messages, the framing layer MUST deliver the enclosed JSON as UTF-8.

#### §2.3.3 Case Sensitivity

Every name defined by this protocol is **case-sensitive** and MUST be reproduced exactly as written. This applies without exception to JSON-RPC member names (such as `jsonrpc`, `id`, `method`, `params`, `result`, `error`, `code`, `message`, `data`), method names and notification names (such as `tools/call`), result-type values, enumeration string values, capability names, reserved metadata keys (such as `io.modelcontextprotocol/protocolVersion`), and any field name appearing in a type definition. A receiver MUST NOT perform case-insensitive matching on any of these names. HTTP header names are matched case-insensitively as required by the HTTP layer, but their canonical spellings are given verbatim where defined.

#### §2.3.4 Forward Compatibility: Unknown Properties

To permit independent evolution of endpoints, receivers MUST ignore object members whose names they do not recognize, rather than rejecting the message, **unless** a specific section of this document states that unknown members in a given object are forbidden or carry defined meaning. "Ignore" means the unrecognized member MUST NOT cause the message to be treated as malformed and MUST NOT alter the receiver's processing of the recognized members; a receiver MAY preserve or forward such members. This rule applies to objects throughout the protocol, including parameter objects, result objects, capability objects, and metadata objects. Senders MUST NOT depend on a receiver acting upon a member the receiver is not required to understand. Correspondingly, the absence of an OPTIONAL member MUST be treated as that member being unset and MUST NOT be treated as an error.

This rule does not relax requirements on members that are **REQUIRED**: a receiver MUST still reject a message that omits a member declared REQUIRED for that message, per the rules of the section defining it.

### §2.4 Type Notation

Wire shapes in this document are written in a TypeScript-like interface notation. This notation describes the structure of JSON values **only**; it names no programming-language type, class, or library, and implementations MAY realize the shapes with whatever native constructs are idiomatic in their language.

The notation uses the following conventions:

- `interface Name { ... }` and `type Name = ...` introduce a named JSON shape for reference elsewhere by name.
- A member written `field: T` is **REQUIRED**: the corresponding object MUST contain a member with that exact name whose value conforms to `T`.
- A member written `field?: T` is **OPTIONAL**: the member MAY be absent; when present, its value MUST conform to `T`. An OPTIONAL member MUST NOT be present with a value of `null` to mean "absent" unless `T` explicitly includes `null`.
- `T | U` is a **union**: the value MUST conform to at least one of the listed alternatives.
- `T[]` is a JSON **array** each of whose elements conforms to `T`.
- `{ [key: string]: T }` is a JSON **object** used as a map with arbitrary string keys, each mapped value conforming to `T`.
- The primitive names `string`, `number`, `boolean`, and `null` denote the corresponding JSON primitive forms; `object` denotes any `JSONObject`; `array` denotes any `JSONArray`. The notation `unknown` and `any` denote any `JSONValue` whose structure is not constrained at that position.
- A string literal such as `"complete"` denotes that exact string value and no other.
- A name used as a member type (for example `Implementation`) refers to the shape defined under that name elsewhere in this document.

Unless a definition states otherwise, an object described by an interface MAY also carry members beyond those listed, subject to the forward-compatibility rule of §2.3.4 Forward Compatibility: Unknown Properties.

### §2.5 Numeric Handling

JSON has a single numeric type. A `number` in this document's notation denotes a JSON number, which represents both integers and real (fractional) values [RFC8259]. Where a field is defined as an integer, senders MUST emit a JSON number with no fractional part and no exponent that would yield a non-integer, and receivers MUST reject a value bearing a fractional component where an integer is required.

For values used as identifiers and counters — including JSON-RPC request identifiers when expressed as numbers, error codes, progress counters, and pagination or sequence counters — senders MUST keep the value within the range of integers exactly representable by IEEE 754 double-precision, that is the inclusive range from −9007199254740991 to 9007199254740991 (−(2^53 − 1) to 2^53 − 1, the safe-integer range). Senders MUST NOT emit an identifier or counter outside this range, and receivers MUST be able to represent and compare every value within it without loss of precision. Error `code` values are integers (see §2.2 Core Terminology and §22 Error Handling and Error Codes) and follow this same integer constraint [JSONRPC2].

Implementations SHOULD NOT rely on a specific decimal serialization of non-integer numbers; two numerically equal JSON numbers MUST be treated as equal regardless of their textual form.

### §2.6 Reserved Names, Extensions, and Forward Compatibility

This protocol reserves certain namespaces so that the base protocol and its extensions can evolve without colliding with implementation-private data [MCP][SEP-2133].

#### §2.6.1 Reserved Method and Notification Prefixes

Method names and notification names are slash-delimited (for example `tools/call`, `notifications/progress`). Names beginning with the segment `notifications/` denote notifications. Implementations MUST NOT define private or extension methods or notifications that collide with names defined by this document. Extensions MUST namespace any method or notification names they introduce so as not to collide with the base protocol or with one another (see §2.6.4 Extensions).

#### §2.6.2 The `_meta` Field and Reserved Metadata Keys

The member name `_meta` is reserved by this protocol wherever it is permitted, to let clients and servers attach additional metadata to their interactions. A `_meta` value is a JSON object (`{ [key: string]: JSONValue }`). Certain key names within `_meta` are reserved for protocol-level metadata; an implementation MUST NOT make assumptions about the values stored under a reserved key beyond what this document specifies for that key. The authoritative definition of the `_meta` object, the complete key-naming rules, and the protocol-defined per-request keys are given in §4 Request Metadata and the Stateless Model; the summary here states only what the reserved-namespace model requires [MCP].

A valid `_meta` key consists of two parts: an OPTIONAL **prefix** followed by a **name**.

**Prefix** — if present:
- It MUST be a series of one or more labels separated by dots (`.`), terminated by a single slash (`/`).
- Each label MUST start with a letter and end with a letter or digit; interior characters MAY be letters, digits, or hyphens (`-`).
- Implementations SHOULD use reverse-DNS notation (for example `com.example/` rather than `example.com/`).
- Any prefix whose **second** label is `modelcontextprotocol` or `mcp` is **reserved** for this protocol. For example, `io.modelcontextprotocol/`, `dev.mcp/`, `org.modelcontextprotocol.api/`, and `com.mcp.tools/` are all reserved, whereas `com.example.mcp/` is NOT reserved because its second label is `example`. Implementations MUST NOT define keys under a reserved prefix except as specified by this document or an MCP-published extension.

**Name** — the portion after the prefix (or the whole key when no prefix is present):
- Unless empty, it MUST begin and end with an alphanumeric character drawn from `[a-z0-9A-Z]`.
- Interior characters MAY be alphanumeric, hyphens (`-`), underscores (`_`), or dots (`.`).

As an exception to the prefix requirement, the bare keys `traceparent`, `tracestate`, and `baggage` are reserved for trace-context propagation; when present, their values MUST conform to the W3C trace-context and baggage formats respectively (see §4 Request Metadata and the Stateless Model) [W3C-TRACE][W3C-BAGGAGE][OTEL][SEP-414].

Keys that are neither reserved nor under a reserved prefix are available for implementation-private use, and receivers MUST ignore `_meta` keys they do not recognize, per §2.3.4 Forward Compatibility: Unknown Properties.

Non-normative example of a `_meta` object carrying a reserved trace key alongside a vendor-namespaced key [W3C-TRACE]:

```json
{
  "_meta": {
    "traceparent": "00-0af7651916cd43dd8448eb211c80319c-00f067aa0ba902b7-01",
    "com.example/tenant": "acme-corp"
  }
}
```

#### §2.6.3 Forward-Compatible Extension of Enumerations and Maps

Wherever this document defines a fixed set of string values (an enumeration) or a map of well-known keys, that set is **open** for extension unless the defining section states it is closed:

- A receiver encountering an enumeration value it does not recognize MUST NOT crash or corrupt unrelated processing. The receiver MUST follow the handling rule given by the section that defines the enumeration. Where that section gives a default-handling rule, the receiver MUST apply it; where the section declares the enumeration closed, an unrecognized value MUST be treated as invalid for that field.
- A sender MUST NOT emit an enumeration value outside the base set unless the corresponding feature or extension is advertised as supported via a capability for that request, and the receiver has declared support for it. The set of acceptable values for an extensible enumeration is the base set defined here together with the values contributed by extensions both peers have agreed upon for the request [SEP-2133].
- Maps of well-known keys MAY likewise be extended with additional keys; receivers MUST ignore unrecognized keys per §2.3.4 Forward Compatibility: Unknown Properties unless the defining section says otherwise.

#### §2.6.4 Extensions

An **extension** is a named, self-contained body of additional functionality negotiated through capabilities. An extension MAY introduce new request methods, notification methods, result-type values, error codes, capabilities, and reserved-prefix metadata keys. An endpoint MUST NOT use a method, notification, result type, error code, or capability contributed by an extension on a given request unless that extension is supported by the peer for that request, as established through capability declaration. An endpoint that does not support an extension MUST handle its absence gracefully and MUST NOT treat an unsupported extension's optional contributions as errors merely because they are unrecognized, subject to the per-feature rules of the sections that define them. The extensions map, its identifier format, and its activation rules are defined in §6 Capabilities and Extensions, and the extension mechanism in full in §24 The Extension Mechanism [SEP-2133][SEP-2596].

## §3 Base Message Format

### §3.1 JSON-RPC Framing

All messages exchanged between clients and servers are individual JSON objects encoded as UTF-8 text and conforming to JSON-RPC 2.0 [JSONRPC2][RFC8259][MCP]. Every message MUST be a single JSON object; senders MUST NOT send JSON-RPC batches (arrays of messages), and receivers that receive a top-level JSON array MUST treat it as a malformed message and reject it with an error (see §22 Error Handling and Error Codes).

Every message MUST contain a member named `jsonrpc` whose value is the exact string `"2.0"`. Receivers MUST reject any message whose `jsonrpc` member is absent or is not exactly `"2.0"` [JSONRPC2].

```ts
// The protocol-level version constant carried by every message.
const JSONRPC_VERSION = "2.0";
```

There are exactly three kinds of message on the wire: **requests**, **notifications**, and **responses**. A message's kind is determined structurally, as defined in §3.3 through §3.6:

```ts
type JSONRPCMessage = JSONRPCRequest | JSONRPCNotification | JSONRPCResponse;
```

A receiver classifies a well-formed message as follows:

- If it carries both an `id` and a `method`, it is a **request**.
- If it carries a `method` but no `id`, it is a **notification**.
- If it carries an `id` and a `result`, it is a **success response**.
- If it carries an `error` (with or without an `id`), it is an **error response**.

A message that carries `method` together with `result` or `error`, or that carries both `result` and `error`, is malformed and MUST be rejected with an error (see §22 Error Handling and Error Codes).

### §3.2 Request Identifier

A request identifier correlates a response with the request that produced it [JSONRPC2][MCP]:

```ts
type RequestId = string | number;
```

The following rules apply to request identifiers:

- A `RequestId` MUST be either a JSON string or a JSON number. It MUST NOT be `null`. (This is a stricter rule than base JSON-RPC, which permits `null`.) [JSONRPC2][MCP]
- A request identifier, once used by a sender on a given connection, MUST NOT be reused by that sender for another request while the original request is still in flight (i.e. before a response with that identifier has been received). Within a single sender on a single connection, identifiers of outstanding requests MUST be unique.
- A receiver MUST include, in the response it returns, the same identifier value (same JSON type and value) that appeared in the request it is answering. Numeric identifiers MUST be returned as numbers and string identifiers as strings; receivers MUST NOT coerce one to the other.
- In this revision only clients originate JSON-RPC requests; servers solicit input by embedding input requests in `"input_required"` results rather than by sending requests (see §11 Multi-Round-Trip Requests). Each sender maintains its own identifier space for the requests it sends. Where an extension or message dialect defines a reverse channel on which both directions originate requests, each side maintains its own identifier space, and a request in one direction and a request in the other may bear the same identifier value without conflict, since they are distinguished by direction.

### §3.3 Requests

A request expects exactly one response [JSONRPC2][MCP]:

```ts
interface Request {
  method: string;
  params?: { [key: string]: any };
}

interface JSONRPCRequest extends Request {
  jsonrpc: "2.0";
  id: RequestId;
}
```

Field reference:

- `jsonrpc` (REQUIRED, string): MUST be `"2.0"`.
- `id` (REQUIRED, `RequestId`): the request identifier, subject to §3.2.
- `method` (REQUIRED, string): the name of the method being invoked. Method names are case-sensitive and MUST be reproduced verbatim. MCP method names are defined in the feature sections of this document.
- `params` (OPTIONAL, object): a JSON object carrying the method arguments. When present it MUST be a JSON object (a map of named members); positional parameters (a JSON array) MUST NOT be used. When a method takes no arguments, `params` MAY be omitted, except where a method's per-request `_meta` is REQUIRED (see §4 Request Metadata and the Stateless Model), in which case `params` MUST be present to carry it. The base shape of `params` common to all requests is defined in §3.7.

A receiver that receives a request whose `method` it does not recognize MUST respond with an error indicating that the method was not found (see §22 Error Handling and Error Codes). A receiver that receives a request whose `params` do not satisfy the method's schema MUST respond with an error indicating invalid parameters (see §22 Error Handling and Error Codes).

### §3.4 Notifications

A notification is a one-way message that does not expect and MUST NOT receive a response [JSONRPC2][MCP]:

```ts
interface Notification {
  method: string;
  params?: { [key: string]: any };
}

interface JSONRPCNotification extends Notification {
  jsonrpc: "2.0";
}
```

Field reference:

- `jsonrpc` (REQUIRED, string): MUST be `"2.0"`.
- `method` (REQUIRED, string): the notification name, case-sensitive and reproduced verbatim. MCP notification names are defined in the feature sections of this document.
- `params` (OPTIONAL, object): a JSON object carrying the notification's data, subject to the same object-only constraint as request `params` (see §3.3). The base shape common to all notifications is defined in §3.7.

A notification MUST NOT contain an `id` member. A receiver MUST NOT send any response (neither success nor error) for a notification, even if the notification is malformed or its `method` is unrecognized; such a notification is silently discarded by the receiver.

### §3.5 Responses

A response answers exactly one request and is either a **success response** or an **error response** [JSONRPC2][MCP]:

```ts
type JSONRPCResponse = JSONRPCResultResponse | JSONRPCErrorResponse;
```

A response MUST contain exactly one of `result` or `error`; it MUST NOT contain both and MUST NOT omit both.

#### §3.5.1 Success Response

```ts
interface JSONRPCResultResponse {
  jsonrpc: "2.0";
  id: RequestId;
  result: Result;
}
```

Field reference:

- `jsonrpc` (REQUIRED, string): MUST be `"2.0"`.
- `id` (REQUIRED, `RequestId`): MUST equal the identifier of the request being answered, per §3.2.
- `result` (REQUIRED, object): the method's result payload, a `Result` object as defined in §3.6.

#### §3.5.2 Error Response

```ts
interface JSONRPCErrorResponse {
  jsonrpc: "2.0";
  id?: RequestId;
  error: Error;
}
```

Field reference:

- `jsonrpc` (REQUIRED, string): MUST be `"2.0"`.
- `id` (OPTIONAL, `RequestId`): the identifier of the request being answered. Senders MUST set `id` to the identifier of the originating request whenever it is known. `id` MAY be omitted only when the sender cannot determine the originating request's identifier — for example, when the incoming text could not be parsed as JSON, or when the message was so malformed that no identifier could be extracted. When present, `id` is subject to §3.2 and MUST equal the originating request's identifier.
- `error` (REQUIRED, object): an `Error` object as defined in §3.8.

### §3.6 Result Base Type

The `result` member of every success response is a `Result` object [SEP-2322][MCP]:

```ts
type ResultType = "complete" | "input_required" | string;

interface Result {
  _meta?: { [key: string]: unknown };
  resultType: ResultType;
  [key: string]: unknown;
}
```

Field reference:

- `_meta` (OPTIONAL, object): a metadata map attached to the result. Its keys are subject to the reserved-key naming rules defined in §4 Request Metadata and the Stateless Model. Receivers MUST NOT make assumptions about the meaning of values stored under MCP-reserved keys they do not understand.
- `resultType` (REQUIRED, string): a discriminator that tells the receiver how to interpret the rest of the result object. Every success response a server emits MUST set `resultType`.
- Additional members: a `Result` MAY carry any number of further members beyond `_meta` and `resultType`; these are defined by the specific method whose result this is.

`ResultType` is an **open set** of string values. Two values are defined by this document [SEP-2322]:

- `"complete"` — the request completed successfully and the result object carries the final content for the method.
- `"input_required"` — the request cannot complete until the client supplies additional input. The result object carries the instructions describing the input the client must provide before re-issuing the original request. The structure of an input-required result and the multi-round-trip flow it drives are defined in §11 Multi-Round-Trip Requests.

Additional `resultType` values MUST NOT be minted except through the extensions mechanism (see §24 The Extension Mechanism) [SEP-2322][SEP-2133]. A receiver that receives a result whose `resultType` value it does not recognize MUST treat that response as an error and MUST NOT attempt to interpret the remaining members of the result object.

The `resultType` member is REQUIRED on every result produced under this protocol revision. A receiver that interoperates with a server that omits `resultType` MUST treat an absent `resultType` as if it were `"complete"`.

### §3.7 Base Request and Notification Params

Every request's `params` extends a common base, and every notification's `params` extends a common base [MCP]:

```ts
interface RequestParams {
  _meta: RequestMetaObject;
}

interface NotificationParams {
  _meta?: { [key: string]: unknown };
}
```

- `RequestParams._meta` (REQUIRED on requests, object): the request metadata object. Unlike most `_meta` members in this protocol, the `_meta` carried by request `params` is REQUIRED, because it conveys per-request protocol state. Its structure, the reserved metadata keys it carries (including the protocol version, client information, per-request capabilities, and the optional desired log level), and the key-naming rules for all `_meta` objects are defined in §4 Request Metadata and the Stateless Model.
- `NotificationParams._meta` (OPTIONAL, object): a metadata map attached to a notification, subject to the same key-naming and reserved-key rules defined in §4 Request Metadata and the Stateless Model.

One reserved member of request `_meta` is referenced from elsewhere in this document: the **progress token**.

```ts
type ProgressToken = string | number;
```

A progress token, when present, is an opaque value the requester supplies so that the receiver MAY emit out-of-band progress notifications correlated to the request. The receiver is not obligated to emit such notifications. The placement of the progress token within request `_meta` and the progress-notification flow are defined in §15 Utilities: Progress, Cancellation, Logging, and Trace Context.

An opaque pagination cursor type is also defined here for reference by paginated methods:

```ts
type Cursor = string;
```

A `Cursor` is an opaque string; receivers MUST NOT parse or infer structure from a cursor value. Its use in list operations is defined in §12 Pagination and by the paginated methods that reference it.

### §3.8 Error Object

The `error` member of an error response is an `Error` object [JSONRPC2][MCP]:

```ts
interface Error {
  code: number;
  message: string;
  data?: unknown;
}
```

Field reference:

- `code` (REQUIRED, number): an integer identifying the error condition. The complete set of defined codes, including the JSON-RPC standard codes and the MCP-specific codes, together with the conditions under which each MUST be used, is defined in §22 Error Handling and Error Codes. Implementations MUST NOT enumerate or assign codes outside the rules given there.
- `message` (REQUIRED, string): a short, human-readable description of the error. The message SHOULD be limited to a single concise sentence.
- `data` (OPTIONAL, any): additional information about the error, whose structure is defined by the sender (for example, structured diagnostics or nested error detail). Receivers MUST NOT assume any particular structure for `data` unless a specific error code defines one in §22 Error Handling and Error Codes.

### §3.9 Empty Result

A method that succeeds but returns no method-specific data returns an empty result, which is a `Result` carrying only the base members:

```ts
type EmptyResult = Result;
```

An `EmptyResult` MUST still set `resultType` (normally to `"complete"`) and MAY carry `_meta`; it carries no additional members.

### §3.10 Examples

A request:

```json
{
  "jsonrpc": "2.0",
  "id": 42,
  "method": "tools/list",
  "params": {
    "_meta": {
      "io.modelcontextprotocol/protocolVersion": "2026-07-28",
      "io.modelcontextprotocol/clientInfo": { "name": "ExampleClient", "version": "1.0.0" },
      "io.modelcontextprotocol/clientCapabilities": {}
    }
  }
}
```

A notification:

```json
{
  "jsonrpc": "2.0",
  "method": "notifications/progress",
  "params": {
    "progressToken": "abc-123",
    "progress": 0.5
  }
}
```

A success response:

```json
{
  "jsonrpc": "2.0",
  "id": 42,
  "result": {
    "resultType": "complete",
    "tools": []
  }
}
```

An error response:

```json
{
  "jsonrpc": "2.0",
  "id": 42,
  "error": {
    "code": -32601,
    "message": "Method not found",
    "data": { "method": "tools/list" }
  }
}
```

An empty success response:

```json
{
  "jsonrpc": "2.0",
  "id": 7,
  "result": {
    "resultType": "complete"
  }
}
```

## §4 Request Metadata and the Stateless Model

This section defines the `_meta` metadata container that carries protocol-level and application-level metadata on messages, the rules for naming `_meta` keys, the protocol-defined per-request `_meta` keys, and the stateless processing model that those keys make possible. It is the authoritative definition of `_meta`; other sections cross-reference it [MCP][SEP-2575][SEP-2567].

### §4.1 The `_meta` Object

The `_meta` field is a reserved property that clients and servers use to attach additional metadata to their interactions. It MAY appear as `_meta` on request `params`, on notification `params`, and on results. Its value is a string-keyed map whose member values MAY be any JSON value [MCP].

In the document's type notation:

```ts
// A string-keyed map of arbitrary metadata.
type MetaObject = { [key: string]: unknown };
```

Rules:

- The `_meta` field is OPTIONAL on any `params` object and on any result, EXCEPT that client requests MUST carry the protocol-defined per-request keys enumerated in §4.3, which live inside `_meta`. A client request therefore MUST include a `_meta` object [SEP-2575].
- Receivers MUST NOT reject a message solely because it carries `_meta` keys the receiver does not recognize; unknown keys MUST be ignored (after the required keys have been validated) [MCP].
- Certain key names are reserved by the protocol for protocol-level metadata. For any reserved key, implementations MUST NOT make assumptions about the value at that key beyond the constraints stated for that key in this document [MCP].
- Individual message schemas MAY reserve particular `_meta` names for purpose-specific metadata; such reservations are stated in the section defining that message.

A `_meta` member value MUST be a valid JSON value [RFC8259]. The `_meta` object itself MUST be a JSON object, not an array or scalar.

### §4.2 Metadata Key Naming Rules

Every `_meta` key is a string. A valid key name has two segments: an OPTIONAL **prefix** followed by a **name** [MCP].

**Prefix.**

- The prefix is OPTIONAL. If present, it MUST be a series of one or more *labels* separated by dots (`.`), terminated by a single slash (`/`).
- Each label MUST start with an ASCII letter and MUST end with an ASCII letter or digit. Interior characters of a label MAY be ASCII letters, digits, or hyphens (`-`).
- Implementations SHOULD form prefixes using reverse-DNS notation derived from a domain they control (for example, `com.example/` rather than `example.com/`).
- A prefix whose **second label** is `modelcontextprotocol` or `mcp` is RESERVED for protocol use. For example, `io.modelcontextprotocol/`, `dev.mcp/`, `org.modelcontextprotocol.api/`, and `com.mcp.tools/` are all reserved. A prefix such as `com.example.mcp/` is NOT reserved, because its second label is `example`. Third parties MUST NOT mint keys under a reserved prefix [MCP].
- The prefix `io.modelcontextprotocol/` is the canonical prefix for keys defined by this document [SEP-2575].

**Name.**

- The name segment, unless empty, MUST begin and end with an alphanumeric character (one of `[a-z0-9A-Z]`).
- Interior characters of the name MAY be alphanumeric, hyphens (`-`), underscores (`_`), or dots (`.`).

**Third-party keys.** A key defined by a vendor or extension that is not the protocol MUST use a non-reserved prefix (a vendor reverse-DNS-style prefix), followed by `/`, followed by a name conforming to the rules above. Example: `com.example/requestTag` [MCP].

**Reserved bare keys.** As an exception to the prefix rule, four bare keys (with no prefix) are RESERVED and MAY appear in `_meta`: `progressToken` (out-of-band progress correlation, see §15), and the three distributed-trace-context keys `traceparent`, `tracestate`, and `baggage`. The trace-context keys' values are opaque to the receiver for routing purposes and MUST be carried unchanged. When present:

- `traceparent` and `tracestate` MUST conform to the W3C Trace Context formats [W3C-TRACE][SEP-414].
- `baggage` MUST conform to the W3C Baggage format [W3C-BAGGAGE][SEP-414].

Receivers that do not participate in tracing MUST ignore these keys; they MUST NOT cause a request to be rejected. Their presence and propagation align with established trace-propagation conventions [OTEL][SEP-414].

### §4.3 Protocol-Defined Per-Request `_meta` Keys

The protocol defines the following keys, all under the reserved prefix `io.modelcontextprotocol/`, for use in the `_meta` object of request `params`. These keys make each request self-describing so it can be processed without reference to any prior request (see §4.4) [SEP-2575].

```ts
// The full shape of a request's params._meta object.
interface RequestMetaObject {
  // Out-of-band progress correlation (see §15 Utilities); OPTIONAL:
  progressToken?: string | number;

  // Distributed-trace-context keys (§4.2), all OPTIONAL:
  traceparent?: string;
  tracestate?: string;
  baggage?: string;

  // Protocol-defined per-request keys:
  "io.modelcontextprotocol/protocolVersion": string;                // REQUIRED on client requests
  "io.modelcontextprotocol/clientInfo": Implementation;             // REQUIRED on client requests
  "io.modelcontextprotocol/clientCapabilities": ClientCapabilities; // REQUIRED on client requests
  "io.modelcontextprotocol/logLevel"?: LoggingLevel;                // OPTIONAL; Deprecated (see §15)

  // Additional protocol-defined or third-party keys MAY also appear (§4.2).
  [key: string]: unknown;
}
```

The keys, verbatim:

| Key (verbatim) | JSON type | Required | Meaning |
| --- | --- | --- | --- |
| `io.modelcontextprotocol/protocolVersion` | `string` | REQUIRED on client requests | The protocol revision the request uses. |
| `io.modelcontextprotocol/clientInfo` | `Implementation` object | REQUIRED on client requests | Identifies the client software issuing the request. |
| `io.modelcontextprotocol/clientCapabilities` | `ClientCapabilities` object | REQUIRED on client requests | The client's capabilities relevant to this request. |
| `io.modelcontextprotocol/logLevel` | `LoggingLevel` string | OPTIONAL | The minimum log severity the server may emit for this request. Status: **Deprecated**. |

**`io.modelcontextprotocol/protocolVersion`** — A string carrying the protocol revision in use for this request; its value is `"2026-07-28"`. A server that does not support the requested revision MUST reject the request with the unsupported-protocol-version error (see §5 Protocol Revision, Version Negotiation, and Discovery for negotiation and the error shape, and §22 Error Handling and Error Codes). For the HTTP transport, this value MUST equal the value carried in the `MCP-Protocol-Version` HTTP header; if they differ, or the header is absent when required, the server MUST respond with HTTP `400 Bad Request` (see §9 The Streamable HTTP Transport) [SEP-2575].

**`io.modelcontextprotocol/clientInfo`** — An `Implementation` object identifying the client software. The `Implementation` object has shape:

```ts
interface Implementation {
  name: string;          // REQUIRED: programmatic identifier of the implementation
  title?: string;        // OPTIONAL: human-readable display name
  version: string;       // REQUIRED: implementation version
  description?: string;  // OPTIONAL: human-readable description of purpose
  websiteUrl?: string;   // OPTIONAL: URI of the implementation's website [RFC3986]
  icons?: Icon[];        // OPTIONAL: visual identifiers (see §14 Common Data Types)
}
```

The `name` and `version` members are REQUIRED; all other members are OPTIONAL. (The full `Implementation` and `Icon` shapes are defined in §14 Common Data Types.) [SEP-2575]

**`io.modelcontextprotocol/clientCapabilities`** — A `ClientCapabilities` object declaring, for this specific request, the optional capabilities the client supports. Capabilities are declared per request rather than once at the start of a connection. An empty object (`{}`) means the client declares no optional capabilities. A server MUST NOT infer client capabilities from any prior request, and MUST NOT rely on a capability the client has not declared in this field. If processing a request requires a client capability that the request did not declare, the server MUST reject the request with the missing-required-client-capability error (JSON-RPC error code `-32003`), whose `data.requiredCapabilities` member lists the missing capabilities; on the HTTP transport the response status MUST be `400 Bad Request` (see §3 Base Message Format for the error envelope, §6 Capabilities and Extensions for the capability catalog, and §5 Protocol Revision, Version Negotiation, and Discovery for the error's full shape). [SEP-2575]

**`io.modelcontextprotocol/logLevel`** — An OPTIONAL `LoggingLevel` string requesting the minimum severity of log messages the server may emit while processing this request. If absent, the server MUST NOT emit log-message notifications for this request; the client opts in to log messages only by setting this key. The permitted values are, in ascending severity: `"debug"`, `"info"`, `"notice"`, `"warning"`, `"error"`, `"critical"`, `"alert"`, `"emergency"`. When set, the server SHOULD emit only log-message notifications at or above the requested severity. This key has status **Deprecated** (see §15 Utilities: Progress, Cancellation, Logging, and Trace Context). [SEP-2577][SEP-2596]

**Validation.** A client request that omits any REQUIRED key in the table above is malformed. The server MUST reject such a request with JSON-RPC error code `-32602` (Invalid params); on the HTTP transport the response status MUST be `400 Bad Request`. [SEP-2575][JSONRPC2]

**Scope.** These per-request keys are mandated on client-originated requests. They are not required on notifications or on results; any of these messages MAY still carry trace keys (§4.2) and other metadata.

### §4.4 The Stateless Model

The protocol is stateless: all information needed to process a request is contained in that request itself. Each request is processed independently, and no state is inferred from any earlier request, including earlier requests carried over the same connection or stream [SEP-2575][SEP-2567].

The following requirements hold:

- There is no separate connection handshake and no session-establishment step that must precede ordinary requests. A request is self-describing through its `_meta` (§4.3): it carries its own protocol version, client identity, and client capabilities [SEP-2575][SEP-2567].
- A server MUST NOT require that any prior request have been sent before it will process a given request [SEP-2575].
- A server MUST NOT infer the client's identity, capabilities, or protocol version from any earlier request; it MUST derive each of these solely from the `_meta` of the request currently being processed [SEP-2575].
- A server MUST NOT depend on persisted per-connection conversational state to process a request. An open connection (for example, a long-lived process or stream) is not a conversation and is not a session; a client MAY interleave unrelated requests on a single connection, and a server MUST NOT treat connection or process identity as a proxy for conversational continuity [SEP-2567].
- Any two requests, even on the same connection, MAY be served by different server instances. A correct server therefore behaves identically regardless of which instance receives a request [SEP-2575].
- Servers SHOULD be prepared to handle requests associated with multiple independent tasks, threads, or conversations arriving over one connection [SEP-2575].
- Servers SHOULD NOT require a client to reuse the same connection or process to perform related operations. Clients SHOULD NOT treat an individual task, thread, or conversation as the lifetime boundary of a connection or process [SEP-2567].

A long-lived request (for example, a streaming notification subscription) remains a single request/response interaction whose state is scoped to the request itself, not to the underlying connection (see §10 Server-to-Client Streaming and Subscriptions and §15 Utilities: Progress, Cancellation, Logging, and Trace Context).

### §4.5 Cross-Call Continuity Without Sessions

When state must span multiple requests — for example a long-running operation or an application-level handle — that state MUST be referenced by an explicit identifier that the client supplies on each subsequent request, never by connection or session identity [SEP-2575][SEP-2567].

- A server mints such an identifier and returns it as an ordinary value in a result (or in a value the client later supplies as a request argument). The identifier is server-minted and opaque [SEP-2575].
- A client MUST treat such an identifier as opaque: it MUST NOT parse, interpret, modify, or construct it, and MUST pass it back verbatim on later requests that operate on the referenced state.
- Because identity flows through these explicit values rather than the connection, the same continuation MAY be driven across different connections, processes, or server instances, consistent with §4.4.

This mechanism is how features that need continuity (such as paginated listings, long-running operations, and subscriptions) carry it; the exact field names and value shapes are defined in the sections that introduce those features.

### §4.6 List Results and Connection Identity

A result that enumerates available items (for example, a listing of tools, resources, or prompts) MUST NOT vary based on connection identity. Two requests bearing the same parameters and the same `_meta` MUST be eligible to receive the same listing irrespective of which connection, process, or server instance handles them. A server MUST NOT use connection or session identity to filter or shape a list result; any variation MUST instead derive from explicit request inputs (such as parameters, pagination cursors, or authenticated identity conveyed per request). [SEP-2575] The per-feature listing operations and their cursor semantics are defined in their respective sections (see §16 Tools, §17 Resources, and §18 Prompts).

### §4.7 Example

The following client request to invoke a tool carries the full reserved per-request `_meta` envelope, including trace context and a third-party key:

```json
{
  "jsonrpc": "2.0",
  "id": 7,
  "method": "tools/call",
  "params": {
    "name": "get_weather",
    "arguments": {
      "location": "New York"
    },
    "_meta": {
      "io.modelcontextprotocol/protocolVersion": "2026-07-28",
      "io.modelcontextprotocol/clientInfo": {
        "name": "example-client",
        "title": "Example Client",
        "version": "1.4.0"
      },
      "io.modelcontextprotocol/clientCapabilities": {},
      "io.modelcontextprotocol/logLevel": "info",
      "traceparent": "00-0af7651916cd43dd8448eb211c80319c-00f067aa0ba902b7-01",
      "tracestate": "vendor1=opaqueValue1,vendor2=opaqueValue2",
      "baggage": "userTier=gold,region=us-east",
      "com.example/requestTag": "checkout-flow"
    }
  }
}
```

A corresponding successful result MAY itself carry `_meta` (for example, trace context echoed for correlation, or a server-minted continuation handle as an ordinary result value), but a result is not required to carry the per-request keys of §4.3:

```json
{
  "jsonrpc": "2.0",
  "id": 7,
  "result": {
    "resultType": "complete",
    "content": [
      { "type": "text", "text": "It is 24°C and sunny in New York." }
    ],
    "isError": false,
    "_meta": {
      "traceparent": "00-0af7651916cd43dd8448eb211c80319c-b9c7c989f97918e1-01"
    }
  }
}
```

## §5 Protocol Revision, Version Negotiation, and Discovery

### §5.1 The Protocol Revision Identifier

The Model Context Protocol identifies each revision of its wire contract with a **protocol revision** string. A protocol revision identifier is a date-formatted string of the form `YYYY-MM-DD` (a four-digit year, a two-digit month, and a two-digit day of month, separated by hyphens `-`). The protocol revision defined by this document has the literal value `"2026-07-28"` [MCP].

Implementations MUST treat protocol revision identifiers as opaque, exactly-matched strings: a server supports a given revision only if the revision string is byte-for-byte equal to one the server implements. Implementations MUST NOT perform lexical, chronological, or range comparison on revision strings to decide support (the date form aids human readability but carries no ordering guarantee for negotiation). [MCP]

There is **no negotiation handshake**. Every request independently declares the protocol revision it is using, and the receiver accepts or rejects that request on its own. A receiver MUST NOT infer the revision of a request from any earlier request. [MCP][SEP-2575][SEP-2567]

### §5.2 Carrying the Protocol Revision on a Request

Every request carries its protocol revision in the reserved request metadata key `io.modelcontextprotocol/protocolVersion`, located in the request's `_meta` object (see §3 Base Message Format and §4 Request Metadata and the Stateless Model for the `_meta` envelope and its reserved keys). This field is REQUIRED on every request. Its JSON type is `string`, and its value MUST be a protocol revision identifier (§5.1). [MCP][SEP-2575]

```ts
// Within a request's params._meta object:
interface RequestProtocolVersionMeta {
  // The MCP protocol revision being used for this request. REQUIRED.
  "io.modelcontextprotocol/protocolVersion": string;
}
```

For the HTTP transport, the same value MUST also be carried in the `MCP-Protocol-Version` HTTP header, and the header value MUST match the `io.modelcontextprotocol/protocolVersion` value in `_meta`; if they do not match, the server MUST respond with HTTP `400 Bad Request` (see §9 The Streamable HTTP Transport for the header and its handling). [MCP][SEP-2575]

A request example declaring the protocol revision:

```json
{
  "jsonrpc": "2.0",
  "id": 7,
  "method": "tools/list",
  "params": {
    "_meta": {
      "io.modelcontextprotocol/protocolVersion": "2026-07-28",
      "io.modelcontextprotocol/clientInfo": { "name": "ExampleClient", "version": "1.0.0" },
      "io.modelcontextprotocol/clientCapabilities": {}
    }
  }
}
```

### §5.3 Discovery: `server/discover`

Servers advertise their supported protocol revisions, capabilities, and identity through the discovery request. Servers MUST implement `server/discover`. [MCP][SEP-2575]

#### §5.3.1 Request

The discovery request uses the method name `server/discover`. Its `params` is an ordinary request-parameters object: it carries no operation-specific fields and consists solely of the standard `_meta` envelope (see §4 Request Metadata and the Stateless Model). The `_meta` object MUST still carry the REQUIRED reserved keys for any request, including `io.modelcontextprotocol/protocolVersion`, `io.modelcontextprotocol/clientInfo`, and `io.modelcontextprotocol/clientCapabilities`. [MCP][SEP-2575]

```ts
interface DiscoverRequest {
  method: "server/discover";
  params: {
    _meta: {
      "io.modelcontextprotocol/protocolVersion": string;                // REQUIRED
      "io.modelcontextprotocol/clientInfo": Implementation;             // REQUIRED; see §14
      "io.modelcontextprotocol/clientCapabilities": ClientCapabilities; // REQUIRED; see §6
      // additional _meta keys MAY be present
    };
  };
}
```

A receiver processing `server/discover` MUST be prepared to receive a protocol revision it does not support, since a client may probe before it knows the server's revisions (§5.7). If the server does not support the requested revision it MUST respond with an `UnsupportedProtocolVersion` error (§5.5); the `data.supported` list still informs the client of the server's revisions.

#### §5.3.2 Result

A successful response to `server/discover` returns a `DiscoverResult`. A `DiscoverResult` is a `Result` (see §3.6 Result Base Type) and therefore carries the base `resultType` member (normally `"complete"`) in addition to its discovery-specific fields. Its fields are: [MCP][SEP-2575]

```ts
interface DiscoverResult extends Result {
  // resultType is inherited from Result and is REQUIRED (normally "complete").

  // REQUIRED. The protocol revisions this server supports. The client
  // selects a revision from this list for subsequent requests.
  supportedVersions: string[];

  // REQUIRED. The capabilities of the server (see §6 Capabilities and Extensions).
  capabilities: ServerCapabilities;

  // REQUIRED. Identity of the server software implementation (see §14
  // Common Data Types).
  serverInfo: Implementation;

  // OPTIONAL. Natural-language guidance describing the server and its
  // features, suitable for inclusion in a host's model context. It SHOULD
  // focus on helping a model use the server effectively and SHOULD NOT
  // duplicate information already present in individual tool descriptions.
  instructions?: string;

  // _meta MAY be present (see §4 Request Metadata and the Stateless Model).
  _meta?: { [key: string]: unknown };
}
```

Field-by-field constraints:

- `resultType` (REQUIRED, string): the base result discriminator, normally `"complete"` (see §3.6 Result Base Type).
- `supportedVersions` (REQUIRED, `string[]`): a non-empty array of protocol revision identifiers (§5.1). Each element MUST be a string the server is willing to accept on subsequent requests. The order of elements is not significant for correctness; clients MUST NOT rely on ordering to determine the server's preference.
- `capabilities` (REQUIRED, `ServerCapabilities`): the server's advertised capabilities object (see §6 Capabilities and Extensions). An empty object `{}` is valid and means the server advertises no optional capabilities.
- `serverInfo` (REQUIRED, `Implementation`): the server's identity; the `Implementation` shape REQUIRES `name` and `version` (both `string`), with other fields OPTIONAL (see §14 Common Data Types).
- `instructions` (OPTIONAL, `string`): if absent, the client MUST NOT assume any guidance.

The wrapping JSON-RPC success response places the `DiscoverResult` in the `result` member:

```ts
interface DiscoverResultResponse {
  jsonrpc: "2.0";
  id: string | number;
  result: DiscoverResult;
}
```

#### §5.3.3 Discovery Examples

`server/discover` request:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "server/discover",
  "params": {
    "_meta": {
      "io.modelcontextprotocol/protocolVersion": "2026-07-28",
      "io.modelcontextprotocol/clientInfo": { "name": "ExampleClient", "version": "1.0.0" },
      "io.modelcontextprotocol/clientCapabilities": {}
    }
  }
}
```

`server/discover` successful result:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "resultType": "complete",
    "supportedVersions": ["2026-07-28"],
    "capabilities": {
      "tools": {},
      "resources": {},
      "extensions": {
        "io.modelcontextprotocol/tasks": {}
      }
    },
    "serverInfo": { "name": "ExampleServer", "version": "2.3.1" },
    "instructions": "This server exposes file-search and code-analysis tools. Prefer search before analysis for large repositories."
  }
}
```

### §5.4 Revision Selection

Discovery is OPTIONAL for clients. A client MAY call `server/discover` before issuing any other request, in order to learn the server's `supportedVersions` and `capabilities` up front; or a client MAY proceed directly, declaring a protocol revision on its first substantive request and handling rejection if that revision is unsupported (§5.5). A client is not required to call `server/discover`. [MCP][SEP-2575]

**Selection rule.** When a client knows the set of revisions the server supports (whether from a `server/discover` result's `supportedVersions`, or from the `supported` array in an `UnsupportedProtocolVersion` error, §5.5), the client MUST select the **highest revision that both the client and the server support** — that is, the revision that is most preferred by the client among those present in the intersection of the client's own supported revisions and the server's advertised revisions. Because revision identifiers are matched exactly (§5.1), "highest" is determined by the client's own ordered list of supported revisions, not by string comparison of dates; the client picks the first revision in its own preference order that also appears in the server's set. [MCP][SEP-2575]

If the intersection is empty (no mutually supported revision exists), the client MUST NOT fabricate a revision. The client SHOULD surface an actionable error to the user or caller indicating incompatibility. [MCP]

The selected revision is then placed in `io.modelcontextprotocol/protocolVersion` (and, on HTTP, in the `MCP-Protocol-Version` header) of every subsequent request (§5.2).

### §5.5 `UnsupportedProtocolVersion` Error

When a request declares a protocol revision in `io.modelcontextprotocol/protocolVersion` that the server does not implement — whether the revision is unknown to the server, or is a revision the server recognizes but does not support — the server MUST reject that request with an `UnsupportedProtocolVersion` error. The error code is the integer `-32004`. For the HTTP transport, the JSON-RPC error response MUST be returned with HTTP status `400 Bad Request`. [MCP][SEP-2575]

The error object's `data` member MUST be present and MUST contain exactly the following fields:

```ts
interface UnsupportedProtocolVersionError {
  jsonrpc: "2.0";
  id: string | number;
  error: {
    code: -32004;
    message: string;
    data: {
      // The protocol revisions the server supports. The client selects a
      // mutually supported revision from this list and retries.
      supported: string[];
      // The protocol revision that was requested by the client.
      requested: string;
    };
  };
}
```

Field constraints:

- `code` (REQUIRED, `number`): MUST be the integer `-32004`.
- `message` (REQUIRED, `string`): a human-readable description; its exact text is not normative.
- `data.supported` (REQUIRED, `string[]`): a non-empty array of protocol revision identifiers (§5.1) the server supports. This is the authoritative set a client uses to re-select.
- `data.requested` (REQUIRED, `string`): the protocol revision string the rejected request declared.

**Client reaction.** On receiving an `UnsupportedProtocolVersion` error, the client SHOULD apply the selection rule (§5.4) against `data.supported`, choose a mutually supported revision, and retry the original request with that revision declared in `io.modelcontextprotocol/protocolVersion` (and the matching `MCP-Protocol-Version` header on HTTP). If no mutually supported revision exists in `data.supported`, the client MUST NOT retry indefinitely; it SHOULD surface an incompatibility error to the user or caller. [MCP][SEP-2575]

Example `UnsupportedProtocolVersion` error:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "error": {
    "code": -32004,
    "message": "Unsupported protocol version",
    "data": {
      "supported": ["2026-07-28"],
      "requested": "1900-01-01"
    }
  }
}
```

### §5.6 `MissingRequiredClientCapability` Error

Client capabilities are declared per request, in the reserved request metadata key `io.modelcontextprotocol/clientCapabilities` (see §4 Request Metadata and the Stateless Model and §6 Capabilities and Extensions). An empty object `{}` means the client declares no optional capabilities for that request, and a server MUST NOT infer client capabilities from any prior request. [MCP][SEP-2575]

When fulfilling a request would require the server to use a client-provided capability that the request did **not** declare in `io.modelcontextprotocol/clientCapabilities`, the server MUST reject the request with a `MissingRequiredClientCapability` error. The error code is the integer `-32003`. For the HTTP transport, the JSON-RPC error response MUST be returned with HTTP status `400 Bad Request`. [MCP][SEP-2575]

The error object's `data` member MUST be present and MUST contain the following field:

```ts
interface MissingRequiredClientCapabilityError {
  jsonrpc: "2.0";
  id: string | number;
  error: {
    code: -32003;
    message: string;
    data: {
      // The capabilities the server requires from the client to process
      // this request (see §6 Capabilities and Extensions for the
      // ClientCapabilities shape).
      requiredCapabilities: ClientCapabilities;
    };
  };
}
```

Field constraints:

- `code` (REQUIRED, `number`): MUST be the integer `-32003`.
- `message` (REQUIRED, `string`): a human-readable description; its exact text is not normative.
- `data.requiredCapabilities` (REQUIRED, `ClientCapabilities`): a client-capabilities object (see §6 Capabilities and Extensions) enumerating the capabilities the server needs the client to declare. The client uses this to decide whether it can satisfy the requirement; if it can, it SHOULD retry the request with those capabilities present in `io.modelcontextprotocol/clientCapabilities`.

Example `MissingRequiredClientCapability` error (the server requires the client to be able to handle an interactive input request, but the original request did not declare that capability):

```json
{
  "jsonrpc": "2.0",
  "id": 42,
  "error": {
    "code": -32003,
    "message": "Required client capability not declared",
    "data": {
      "requiredCapabilities": {
        "elicitation": {}
      }
    }
  }
}
```

### §5.7 Backward-Compatibility Probe for Transports Without Out-of-Band Negotiation

Some transports (such as stdio; see §8 The stdio Transport) provide no out-of-band mechanism for a client to learn whether a server speaks this protocol revision before exchanging messages. On such transports, a peer MAY use `server/discover` — or the manner in which it fails — to detect protocol support. [MCP][SEP-2575]

A client probing a server SHOULD send `server/discover` as its opening request:

- If the response is a successful `DiscoverResult` (§5.3.2), the server supports this protocol revision family; the client reads `supportedVersions` and applies the selection rule (§5.4).
- If the response is a recognized protocol error of this revision — specifically an `UnsupportedProtocolVersion` error (§5.5) carrying `data.supported` — the server supports this protocol revision family but not the requested revision; the client re-selects from `data.supported` (§5.4) rather than abandoning the protocol.
- If the response is any error that is **not** a recognized error of this protocol revision (for example, an unknown-method error, a malformed response, or no response within the transport's timeout), the client MUST treat the server as not speaking this protocol revision and MAY fall back to whatever alternative behavior it supports, surfacing an actionable error if it has none.

The protocol-support determination is a property of the server endpoint, not of an individual request. A client SHOULD cache the result for the lifetime of the connected server process and MAY persist it across restarts of the same server configuration, re-probing if a cached assumption later proves wrong (for example, if a revision the server reported as supported begins returning `UnsupportedProtocolVersion`). [MCP][SEP-2575]

A server that implements only this protocol revision family, when it receives an opening request shaped for an out-of-band-negotiated transport that it cannot interpret, SHOULD name the protocol revisions it supports in any error it returns, on any transport, so that a peer with no fall-forward mechanism can still surface a useful diagnostic. [MCP][SEP-2575]

## §6 Capabilities and Extensions

This section defines the capability objects exchanged between peers, the rules governing capability declaration and negotiation, and the extensions mechanism for advertising support beyond the core protocol [MCP][SEP-2133]. A capability object is a JSON object whose presence and structure declare which families of methods and behaviors a peer supports. The two capability objects are `ClientCapabilities` (declared by the client) and `ServerCapabilities` (declared by the server).

The transport of these objects is per-request and stateless: a client conveys its `ClientCapabilities` on every request via the `io.modelcontextprotocol/clientCapabilities` key inside the request `_meta` (see §4 Request Metadata and the Stateless Model), and a client learns a server's `ServerCapabilities` from the `server/discover` result (see §5 Protocol Revision, Version Negotiation, and Discovery) [SEP-2575][SEP-2567]. No prior connection state is required or permitted to infer capabilities.

### §6.1 Capability Semantics

A capability declares support for a family of related methods and the behaviors associated with it. A peer MUST NOT depend on a behavior, method, or notification belonging to a capability that the other peer has not declared.

The following principles apply to all capability objects [MCP]:

- A capability is declared by the **presence** of its named field. The presence of the field — even when its value is an empty object `{}` — signifies "this capability is supported." Absence of the field signifies "this capability is not supported."
- A capability field's value, when it is an object, MAY contain **sub-flags**: nested fields that declare optional behaviors WITHIN that capability family. A sub-flag set to `true` declares the optional behavior is supported; a sub-flag that is absent, or set to `false`, declares it is not supported. Sub-flags refine but do not replace the meaning of the enclosing capability's presence.
- A capability MUST NOT be inferred from the use of a related capability. Each behavior is gated by its own declared capability or sub-flag.

A peer that declares a capability MUST be prepared to receive any method or notification in that capability's family that does not require an undeclared sub-flag. A peer MUST NOT declare a capability it does not support.

### §6.2 ClientCapabilities

`ClientCapabilities` declares the behaviors the client supports. The client supplies this object on every request (see §4 Request Metadata and the Stateless Model) so that a server can determine, per request, which client-side behaviors it may invoke [SEP-2575].

```ts
interface ClientCapabilities {
  // Non-standard, experimental capabilities the client supports.
  // Keys are arbitrary capability names; values are settings objects.
  experimental?: { [key: string]: object };

  // Present if the client supports listing filesystem roots.
  // Status: Deprecated. See §21 Deprecated Client-Provided Capabilities.
  roots?: {};

  // Present if the client supports server-initiated LLM sampling.
  // Status: Deprecated. See §21 Deprecated Client-Provided Capabilities.
  sampling?: {
    // Present if the client supports context inclusion via the
    // sampling `includeContext` parameter.
    context?: object;
    // Present if the client supports tool use via the sampling
    // `tools` and `toolChoice` parameters.
    tools?: object;
  };

  // Present if the client supports server-initiated elicitation.
  elicitation?: {
    // Present if the client supports form-mode elicitation.
    form?: object;
    // Present if the client supports URL-mode elicitation.
    url?: object;
  };

  // Optional MCP extensions the client supports (see §6.5).
  extensions?: { [key: string]: object };
}
```

Field definitions:

- **`experimental`** (OPTIONAL, object map). A map whose keys are non-standard capability identifiers and whose values are settings objects of arbitrary structure. This map carries capabilities outside the core protocol that do not use the structured `extensions` mechanism. Receivers MUST ignore keys they do not recognize. Senders MUST NOT depend on any `experimental` key being understood by the receiver.

- **`elicitation`** (OPTIONAL, object). Present if the client supports server-initiated elicitation requests, in which a server asks the client to gather information from the user (see §20 Elicitation). Sub-flags:
  - **`form`** (OPTIONAL, object). Present if the client supports form-mode elicitation, in which the server supplies a schema describing structured fields to be collected. When `elicitation` is present but `form` is absent, a client is understood to support form mode implicitly as the baseline behavior.
  - **`url`** (OPTIONAL, object). Present if the client supports URL-mode elicitation, in which the server directs the client to open a URL to complete an interaction. A server MUST NOT use URL-mode elicitation unless `url` is present.
  Each sub-flag value, when present, is an object; an empty object `{}` means "this elicitation mode is supported, with no further settings."

- **`roots`** (OPTIONAL, object). Status: **Deprecated**. Present if the client supports exposing filesystem roots to the server via the `roots/list` method and the corresponding list-changed notification. Its value is an empty object `{}`. A server MUST NOT invoke `roots/list` unless this capability is present. New implementations SHOULD NOT rely on this capability; see §21 Deprecated Client-Provided Capabilities [SEP-2577].

- **`sampling`** (OPTIONAL, object). Status: **Deprecated**. Present if the client supports server-initiated LLM sampling via the `sampling/createMessage` method, in which a server asks the client to obtain a model completion. A server MUST NOT invoke `sampling/createMessage` unless this capability is present. New implementations SHOULD NOT rely on this capability; see §21 Deprecated Client-Provided Capabilities [SEP-2577]. Sub-flags:
  - **`context`** (OPTIONAL, object). Present if the client supports context inclusion via the sampling `includeContext` parameter. When this sub-flag is absent, a server SHOULD only use `includeContext: "none"` or omit `includeContext` entirely.
  - **`tools`** (OPTIONAL, object). Present if the client supports tool use within sampling via the sampling `tools` and `toolChoice` parameters. A server MUST NOT supply those parameters unless this sub-flag is present.
  Each sub-flag value, when present, is an object; an empty object `{}` means "supported, with no further settings."

- **`extensions`** (OPTIONAL, object map). The map of MCP extensions the client supports, as defined in §6.5 The Extensions Map [SEP-2133].

A client MAY omit any field; an omitted `ClientCapabilities` field declares that the corresponding behavior is not supported. An entirely empty object `{}` is a valid `ClientCapabilities` value declaring no optional client behaviors.

### §6.3 ServerCapabilities

`ServerCapabilities` declares the behaviors the server supports. A client learns this object from the `server/discover` result (see §5 Protocol Revision, Version Negotiation, and Discovery) and uses it to determine which server-side methods it may invoke [SEP-2575].

```ts
interface ServerCapabilities {
  // Non-standard, experimental capabilities the server supports.
  experimental?: { [key: string]: object };

  // Present if the server supports sending log messages to the client.
  // Status: Deprecated. See §15 Utilities: Progress, Cancellation, Logging, and Trace Context (§15.3).
  logging?: object;

  // Present if the server supports argument autocompletion suggestions.
  completions?: object;

  // Present if the server offers prompt templates.
  prompts?: {
    // Whether the server emits notifications when the prompt list changes.
    listChanged?: boolean;
  };

  // Present if the server offers resources to read.
  resources?: {
    // Whether the server supports subscribing to individual resource updates.
    subscribe?: boolean;
    // Whether the server emits notifications when the resource list changes.
    listChanged?: boolean;
  };

  // Present if the server offers tools to call.
  tools?: {
    // Whether the server emits notifications when the tool list changes.
    listChanged?: boolean;
  };

  // Optional MCP extensions the server supports (see §6.5).
  extensions?: { [key: string]: object };
}
```

Field definitions:

- **`experimental`** (OPTIONAL, object map). A map whose keys are non-standard capability identifiers and whose values are settings objects of arbitrary structure. Receivers MUST ignore keys they do not recognize. Senders MUST NOT depend on any `experimental` key being understood by the receiver.

- **`completions`** (OPTIONAL, object). Present if the server supports argument autocompletion via the `completion/complete` method (see §19 Completion). Its value is an object; an empty object `{}` declares support with no further settings. A client MUST NOT invoke `completion/complete` unless this capability is present.

- **`prompts`** (OPTIONAL, object). Present if the server offers prompt templates via the `prompts/list` and `prompts/get` methods (see §18 Prompts). Sub-flag:
  - **`listChanged`** (OPTIONAL, boolean). When `true`, the server emits the `notifications/prompts/list_changed` notification when its set of available prompts changes. When absent or `false`, the server does not emit that notification, and the client MUST NOT expect it.

- **`resources`** (OPTIONAL, object). Present if the server offers resources via the `resources/list`, `resources/read`, and related methods (see §17 Resources). Sub-flags:
  - **`subscribe`** (OPTIONAL, boolean). When `true`, the server supports per-resource update notifications (`notifications/resources/updated`) for resources a client opts into via the `resourceSubscriptions` filter of a `subscriptions/listen` stream (see §10 Server-to-Client Streaming and Subscriptions and §17.7). When absent or `false`, the server does not deliver per-resource update notifications.
  - **`listChanged`** (OPTIONAL, boolean). When `true`, the server emits the `notifications/resources/list_changed` notification when its set of available resources changes. When absent or `false`, the client MUST NOT expect that notification.

- **`tools`** (OPTIONAL, object). Present if the server offers tools via the `tools/list` and `tools/call` methods (see §16 Tools). Sub-flag:
  - **`listChanged`** (OPTIONAL, boolean). When `true`, the server emits the `notifications/tools/list_changed` notification when its set of available tools changes. When absent or `false`, the client MUST NOT expect that notification.

- **`logging`** (OPTIONAL, object). Status: **Deprecated**. Present if the server supports sending structured log messages to the client via the `notifications/message` notification (see §15 Utilities: Progress, Cancellation, Logging, and Trace Context). Its value is an object; an empty object `{}` declares support with no further settings. New implementations SHOULD NOT rely on this capability; see §15.3 Logging [SEP-2577].

- **`extensions`** (OPTIONAL, object map). The map of MCP extensions the server supports, as defined in §6.5 The Extensions Map [SEP-2133].

A server MAY omit any field; an omitted `ServerCapabilities` field declares that the corresponding behavior is not supported. An entirely empty object `{}` is a valid `ServerCapabilities` value declaring no optional server behaviors.

### §6.4 Per-Request Capability Negotiation

A feature is usable on the wire only when BOTH peers have declared the relevant capability — the feature's effective availability is the intersection of what each side supports [MCP][SEP-2575].

The following rules are normative:

1. **Receiver must declare before invocation.** A sender MUST NOT invoke a method, send a notification, or rely on a behavior whose governing capability or sub-flag the receiver has not declared.

2. **Servers learn client capabilities per request.** Before a server emits any input request (e.g. elicitation) inside a result, or sends a notification, the server MUST consult the `ClientCapabilities` carried in the `io.modelcontextprotocol/clientCapabilities` field of the current request's `_meta` (see §4 Request Metadata and the Stateless Model). The server MUST NOT infer client capabilities from any prior request, connection, or process [SEP-2575][SEP-2567]. A server MUST NOT rely on a capability the client did not include in that field.

3. **Input requests are scoped to the originating request.** When a server includes input requests in an `"input_required"` result while processing a client request, those input requests are governed by the capabilities the client declared on the originating request (see §4 Request Metadata and the Stateless Model and §11 Multi-Round-Trip Requests) [SEP-2260][SEP-2322].

4. **Clients learn server capabilities from discovery.** Before a client invokes any server method, the client MUST consult the `ServerCapabilities` obtained from the most recent `server/discover` result (see §5 Protocol Revision, Version Negotiation, and Discovery). A client MUST NOT invoke a server method whose governing capability the server did not declare [SEP-2575].

5. **Missing-capability error.** If processing a request requires a capability the client did not declare in `io.modelcontextprotocol/clientCapabilities`, the server MUST reject the request with the error code `-32003` (a missing-required-client-capability error), whose `error.data` object includes a `requiredCapabilities` field listing the capabilities that were required but not declared. On an HTTP-based transport, the response status code MUST be `400 Bad Request` (see §5 Protocol Revision, Version Negotiation, and Discovery, §22 Error Handling and Error Codes, and §9 The Streamable HTTP Transport). A request that omits any required `_meta` field is malformed and MUST be rejected with error code `-32602` (Invalid params); on HTTP the status MUST be `400 Bad Request` [MCP].

6. **Graceful degradation.** A peer that supports an optional behavior the other peer does not MUST fall back to core behavior whose governing capability both sides declare, or — only when the unsupported behavior is mandatory for the operation — reject the request with an appropriate error. A peer MUST NOT fail merely because the other peer declared fewer capabilities than it supports.

### §6.5 The Extensions Map

The `extensions` map advertises support for MCP extensions: optional, independently versioned additions to the protocol that define behavior beyond the core specification [SEP-2133]. It appears in both `ClientCapabilities` and `ServerCapabilities`.

```ts
// In both ClientCapabilities and ServerCapabilities:
extensions?: { [key: string]: object };
```

**Key format.** Each key is an extension identifier formed as a reverse-DNS-style prefix, followed by a slash (`/`), followed by an extension name [SEP-2133]:

- The **prefix** is REQUIRED for extension identifiers. It is a series of labels separated by dots (`.`). Each label MUST start with a letter and end with a letter or digit; interior characters MAY be letters, digits, or hyphens (`-`). Reverse-DNS notation (e.g. `com.example`) is RECOMMENDED so that a party in control of a domain name controls a unique prefix.
- A single slash (`/`) separates the prefix from the name.
- The **name** identifies the extension within its prefix. Unless empty, the name MUST begin and end with an alphanumeric character (`[a-zA-Z0-9]`) and MAY contain hyphens (`-`), underscores (`_`), dots (`.`), and alphanumerics in between.

Examples of well-formed identifiers: `io.modelcontextprotocol/tasks`, `io.modelcontextprotocol/oauth-client-credentials`, `io.modelcontextprotocol/ui`, `com.example/my-extension`.

**Reserved prefixes.** Any prefix whose second label is `modelcontextprotocol` or `mcp` is RESERVED for official MCP use; third parties MUST NOT define extensions under such prefixes. For example, `io.modelcontextprotocol`, `dev.mcp`, `org.modelcontextprotocol.api`, and `com.mcp` are all reserved. A prefix is NOT reserved merely because `mcp` or `modelcontextprotocol` appears as some other label; for example `com.example.mcp` is not reserved, because its second label is `example` [SEP-2133].

**Values.** Each value is an extension-specific settings object whose structure is defined by the extension. The following rules are normative:

- An empty object `{}` means "this extension is enabled, with no settings." It MUST be treated as a valid enabling declaration, not as absence.
- A key MUST NOT map to `null`. A receiver encountering a `null` value MUST treat that entry as malformed and MUST ignore it (i.e. treat the extension as not advertised by that peer).
- Each extension specifies the schema of its own settings object; settings keys not defined by an extension MUST be ignored by receivers of that extension.

**Activation by intersection.** An extension is active for an interaction only when it appears in BOTH peers' advertised `extensions` maps — that is, an extension is active only in the intersection of the set of identifiers the client advertises and the set the server advertises [SEP-2133]. A peer MUST NOT exercise extension behavior with a peer that did not advertise the same extension identifier. Extensions are disabled by default; a peer advertises an extension only when it has been explicitly enabled.

**Behavior when only one side supports an extension.** When one peer advertises an extension the other does not, the supporting peer MUST either fall back to core protocol behavior or, only if the extension is mandatory for the operation, reject the request with an appropriate error.

The full extension mechanism, including how extensions contribute methods, notifications, result types, and error codes, is defined in §24 The Extension Mechanism [SEP-2133].

### §6.6 Forward Compatibility

Capability objects and the `extensions` map are open for extension; a peer MUST tolerate fields and keys it does not recognize [MCP][SEP-2133]:

- A receiver MUST ignore any capability field in `ClientCapabilities` or `ServerCapabilities` that it does not recognize. The presence of an unknown capability field MUST NOT cause a peer to reject the capability object or the message carrying it.
- A receiver MUST ignore any key in the `extensions` map (and any key in the `experimental` map) whose identifier it does not recognize, and MUST treat such an entry as though the extension is simply not active in the intersection.
- A receiver MUST ignore settings keys within a recognized extension's settings object that it does not recognize, to allow extensions to add settings over time without breaking older receivers.
- Unknown capabilities, unknown extensions, and unknown settings MUST NOT be treated as errors. A peer MUST NOT assume that the absence of a field it does not understand implies non-support of anything it does understand.

### §6.7 Examples

A `ClientCapabilities` object declaring elicitation in both form and URL modes, plus support for one extension with settings:

```json
{
  "elicitation": {
    "form": {},
    "url": {}
  },
  "extensions": {
    "io.modelcontextprotocol/ui": {
      "mimeTypes": ["text/html;profile=mcp-app"]
    }
  }
}
```

The same object as it appears on the wire inside a request's `_meta` (see §4 Request Metadata and the Stateless Model):

```json
{
  "jsonrpc": "2.0",
  "id": 7,
  "method": "tools/call",
  "params": {
    "name": "get_weather",
    "arguments": { "location": "New York" },
    "_meta": {
      "io.modelcontextprotocol/protocolVersion": "2026-07-28",
      "io.modelcontextprotocol/clientInfo": { "name": "ExampleClient", "version": "1.0.0" },
      "io.modelcontextprotocol/clientCapabilities": {
        "elicitation": { "form": {}, "url": {} },
        "extensions": {
          "io.modelcontextprotocol/ui": { "mimeTypes": ["text/html;profile=mcp-app"] }
        }
      }
    }
  }
}
```

A `ServerCapabilities` object declaring tools with list-changed notifications, resources with both subscribe and list-changed, prompts, completions, and one extension advertised with no settings:

```json
{
  "tools": {
    "listChanged": true
  },
  "resources": {
    "subscribe": true,
    "listChanged": true
  },
  "prompts": {
    "listChanged": false
  },
  "completions": {},
  "extensions": {
    "io.modelcontextprotocol/tasks": {}
  }
}
```

The same object as it appears in a `server/discover` result (see §5 Protocol Revision, Version Negotiation, and Discovery):

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "resultType": "complete",
    "supportedVersions": ["2026-07-28"],
    "serverInfo": { "name": "ExampleServer", "version": "1.0.0" },
    "capabilities": {
      "tools": { "listChanged": true },
      "resources": { "subscribe": true, "listChanged": true },
      "prompts": { "listChanged": false },
      "completions": {},
      "extensions": {
        "io.modelcontextprotocol/tasks": {}
      }
    }
  }
}
```

In the two examples above, the `io.modelcontextprotocol/ui` extension is active only if the server also advertises `io.modelcontextprotocol/ui`, and the `io.modelcontextprotocol/tasks` extension is active only if the client also advertises `io.modelcontextprotocol/tasks`; otherwise each peer MUST fall back to core behavior or reject mandatory operations as described in §6.5 The Extensions Map [SEP-2133][SEP-2663].

# Part II — Transports

## §7 Transport Model

A transport is the substrate that carries Model Context Protocol messages between a client and a server. The protocol itself defines what messages mean and how exchanges are sequenced; a transport defines only how the bytes of those messages are framed, delivered, and torn down. Protocol semantics are identical on every transport: the messages, their meaning, and the exchange patterns (see §3 Base Message Format) are part of the core protocol and do not vary by transport binding [MCP].

### §7.1 What a Transport Provides

A transport MUST provide a bidirectional channel that carries the JSON-RPC messages defined in §3 Base Message Format. The set of messages a transport carries is exactly the union type [MCP][JSONRPC2]:

```ts
type JSONRPCMessage = JSONRPCRequest | JSONRPCNotification | JSONRPCResponse;
```

where `JSONRPCResponse` covers both the success form (carrying `result`) and the error form (carrying `error`), as defined in §3 Base Message Format.

A transport MUST:

- Carry each message as a complete UTF-8-encoded JSON value [RFC8259][RFC4648]. See §7.6 Character Encoding and Statelessness.
- Preserve message integrity: a message delivered to a receiver MUST be byte-for-byte the JSON value the sender emitted (after transport-specific framing is removed), so that no field is added, dropped, reordered into invalidity, or corrupted in transit.
- Deliver messages in both directions: from client to server, and from server to client. See §7.4 Directionality.

A transport does NOT define the meaning of any method, the structure of any `params` or `result`, capability negotiation, or version negotiation; those are specified by the core protocol and are carried unchanged on every transport [MCP].

### §7.2 Transport-Agnostic Guarantees and Responsibilities

Every transport, whether one of the two defined in this document or a custom transport (see §7.3 Defined and Custom Transports), MUST uphold the following guarantees. These are the contract on which the rest of this document relies.

**Message framing and delimitation.** A transport MUST define an unambiguous way to determine, for each delivered unit, the exact byte boundaries of one JSON-RPC message, so that a receiver can recover the complete message and parse it as a single JSON value [RFC8259]. A receiver MUST NOT be required to parse the JSON body in order to find where one message ends and the next begins; the framing alone MUST delimit messages. (The stdio transport delimits messages by newlines; the Streamable HTTP transport delimits messages by HTTP message bodies and Server-Sent Events `data` fields. See §8 The stdio Transport and §9 The Streamable HTTP Transport.)

**Association of responses to requests by id.** Each `JSONRPCRequest` carries an `id` (a string or number, and never `null`; see §3 Base Message Format). A receiver that returns a response MUST set the response `id` to the same value as the request `id`, and a sender MUST use the request `id` to associate an inbound response with the originating request. Correlation is by `id` and MUST NOT depend on delivery order, on which connection or stream carried the response, or on any transport-level position. A `JSONRPCNotification` carries no `id` and is never correlated to a response (see §10 Server-to-Client Streaming and Subscriptions). The single permitted exception is an error response to a request whose `id` could not be read because the message was malformed; in that case the error response MAY carry a `null` id or omit the id, as specified in §3 Base Message Format and §22 Error Handling and Error Codes.

**Multiplexing of concurrent requests.** A sender MAY have multiple requests outstanding at the same time over a single transport connection. To enable correlation, a sender MUST NOT reuse the `id` of any request for which it has issued a request and not yet received a response [MCP][JSONRPC2]. A transport MUST permit such concurrent outstanding requests and MUST NOT require that a sender await the response to one request before issuing another.

**Response ordering.** Responses MAY arrive in any order relative to the order in which their requests were sent. A receiver of responses MUST NOT assume that responses arrive in request order, and MUST rely solely on `id` correlation (see above). A transport MUST NOT impose first-in-first-out response ordering as a precondition for correct operation.

**No silent loss.** A transport MUST NOT silently drop messages. Every message accepted from a sender MUST either be delivered to the receiver or result in an observable failure (for example, a transport-level error surfaced to the sender, or an observable disconnection; see §7.5 Error and Disconnection Handling). A transport MUST NOT discard a message and continue as though it were delivered.

**Clean shutdown and close.** A transport MUST define how a connection is closed cleanly and how each side observes that the channel is not usable. When a transport connection is closed, in-flight requests that have not received a response are treated as failed (see §7.5 Error and Disconnection Handling). A clean close is one in which each side has the opportunity to observe the close rather than inferring it only from a later failed operation. Transport-specific close mechanics are defined in §8 The stdio Transport (closing the subprocess input stream and awaiting exit) and §9 The Streamable HTTP Transport (completing or closing the relevant HTTP exchanges).

### §7.3 Defined and Custom Transports

The protocol is transport-agnostic and can be implemented over any communication channel that supports bidirectional message exchange [MCP]. This document defines two transports:

- The **stdio transport** (see §8 The stdio Transport): newline-delimited JSON-RPC messages over the standard input and output streams of a server subprocess launched by the client.
- The **Streamable HTTP transport** (see §9 The Streamable HTTP Transport): each message is delivered via HTTP to a single MCP endpoint, with replies returned as a JSON object or as a request-scoped Server-Sent Events stream [HTML-SSE].

Clients and servers MAY implement additional custom transport mechanisms to suit their specific needs. Implementers who choose to support a custom transport MUST preserve the JSON-RPC message format (see §3 Base Message Format), the message exchange patterns (see §3 Base Message Format), and the per-request metadata model (see §4 Request Metadata and the Stateless Model), and MUST uphold every guarantee in §7.2 Transport-Agnostic Guarantees and Responsibilities. A custom transport SHOULD document its connection establishment, message framing, and cancellation patterns to aid interoperability [MCP].

A custom transport that runs over a reliable bidirectional byte stream (for example, a Unix domain socket or a TCP connection) SHOULD reuse the stdio framing rather than defining a new one: that framing is newline-delimited JSON-RPC over a byte stream, and only its process-lifecycle rules are specific to standard streams (see §8 The stdio Transport) [MCP].

### §7.4 Directionality

Both clients and servers send and receive messages over the transport; the channel is bidirectional. A transport MUST be capable of carrying messages in both directions over the lifetime of a single connection [MCP].

Within that bidirectional channel, the permitted message directions are constrained by the message exchange patterns (see §3 Base Message Format):

- A transport MUST deliver client-sent **requests** and **notifications** to the server.
- A transport MUST deliver server-sent **responses** (results and errors) and **notifications** to the client.

No other message direction exists at the JSON-RPC layer: servers do not initiate JSON-RPC requests, and clients do not send JSON-RPC responses [MCP]. A server that needs additional input from the client to complete a request does so within the result of the in-flight request rather than by initiating a new request (multi-round-trip requests; see §11 Multi-Round-Trip Requests) [SEP-2322][SEP-2260]. Server-originated notifications are emitted only under the rules of §10 Server-to-Client Streaming and Subscriptions.

The per-request metadata envelope travels with every request, on every transport. Each request MUST carry, in its `_meta` object, the protocol version, client identity, and client capabilities for that request, under the reserved `io.modelcontextprotocol/*` keys defined in §4 Request Metadata and the Stateless Model. A transport MAY additionally mirror selected envelope fields into transport-level metadata for routing and inspection (the Streamable HTTP transport mirrors them into HTTP headers; see §9 The Streamable HTTP Transport), but the message body remains the source of truth, and the inline `_meta` envelope is REQUIRED regardless of transport [SEP-2575][SEP-2243].

### §7.5 Error and Disconnection Handling

Errors at the transport level are distinct from JSON-RPC error responses. A JSON-RPC error response (carrying an `error` object; see §22 Error Handling and Error Codes) is a normal, fully delivered protocol message reporting that a request failed at the protocol or application layer. A transport-level error is a failure of the channel itself.

**Observing abrupt disconnection.** A transport MUST make abrupt disconnection observable to each side. Disconnection is observed through the transport-specific mechanism: for the stdio transport, the server subprocess exiting or the standard streams closing or returning end-of-file (see §8 The stdio Transport); for the Streamable HTTP transport, the underlying HTTP connection or SSE stream closing or erroring (see §9 The Streamable HTTP Transport). An implementation MUST surface such a disconnection rather than blocking indefinitely as though the channel were still live.

**In-flight requests on disconnection.** When a transport connection is lost, every request that the sender has issued and for which no response has been received MUST be considered failed. The sender MUST NOT wait indefinitely for responses that can never arrive; it MUST resolve such requests as failures so that callers can observe the error and, where appropriate, retry. Because the protocol is stateless (see §4 Request Metadata and the Stateless Model and §7.6 Character Encoding and Statelessness), a failed in-flight request carries no residual state on the connection, and the client MAY retry it against a fresh connection or process. For the stdio transport, if the server process exits unexpectedly the client SHOULD restart it; in-flight requests are lost and MAY be retried against the fresh process (see §8 The stdio Transport) [MCP].

**No silent drops on error.** Consistent with §7.2 Transport-Agnostic Guarantees and Responsibilities, a transport MUST NOT respond to an error condition by silently discarding a message. A message that cannot be delivered MUST produce an observable failure on the affected side.

### §7.6 Character Encoding and Statelessness

**Character encoding.** All JSON-RPC messages carried by any transport MUST be UTF-8 encoded [RFC8259][RFC4648]. A receiver MUST reject, as a transport-level or parse-level error, any unit that is not well-formed UTF-8 or that does not parse as a single JSON value, and MUST NOT silently substitute or drop it.

**No connection-scoped conversational state.** A single transport connection MUST NOT be required to carry conversational state across requests. The protocol is stateless: all information needed to process a request is contained in that request, and a server MUST NOT infer state from prior requests, even those on the same connection or stream (see §4 Request Metadata and the Stateless Model) [SEP-2575][SEP-2567]. Specifically:

- A server MUST NOT rely on prior requests over the same connection to establish context such as capabilities, protocol version, or client identity; every request supplies this metadata in its `_meta` field (see §4 Request Metadata and the Stateless Model).
- A server SHOULD NOT require that a client reuse the same connection or process to perform related operations.
- A client MAY interleave unrelated requests on the same transport connection; the connection or process identity MUST NOT be treated as a proxy for conversation or session continuity.
- State that must span multiple requests MUST be referenced by an explicit identifier that the client passes on each request, not by connection identity (see §4 Request Metadata and the Stateless Model and §15 Utilities).

A long-lived exchange such as a subscription stream remains a single request/response whose state is scoped to the request, not to the connection underneath it; see §3 Base Message Format and §17 Resources [SEP-2575].

### §7.7 Examples

The following request travels identically on any transport. The framing differs (a single newline-terminated line on stdio; an HTTP request body or SSE `data` field on Streamable HTTP), but the JSON value carried is the same, and the per-request `_meta` envelope (see §4 Request Metadata and the Stateless Model) is present regardless of transport.

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "get_weather",
    "arguments": { "location": "New York" },
    "_meta": {
      "io.modelcontextprotocol/protocolVersion": "2026-07-28",
      "io.modelcontextprotocol/clientInfo": { "name": "example-client", "version": "1.0.0" },
      "io.modelcontextprotocol/clientCapabilities": {}
    }
  }
}
```

The corresponding response carries the same `id`, demonstrating §7.2 association by id:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "resultType": "complete",
    "content": [{ "type": "text", "text": "Sunny, 24C" }]
  }
}
```

Multiplexing and out-of-order delivery: a client sends two requests without awaiting the first response.

```json
{ "jsonrpc": "2.0", "id": 2, "method": "tools/list", "params": { "_meta": { "io.modelcontextprotocol/protocolVersion": "2026-07-28", "io.modelcontextprotocol/clientInfo": { "name": "example-client", "version": "1.0.0" }, "io.modelcontextprotocol/clientCapabilities": {} } } }
```

```json
{ "jsonrpc": "2.0", "id": 3, "method": "resources/list", "params": { "_meta": { "io.modelcontextprotocol/protocolVersion": "2026-07-28", "io.modelcontextprotocol/clientInfo": { "name": "example-client", "version": "1.0.0" }, "io.modelcontextprotocol/clientCapabilities": {} } } }
```

The responses MAY arrive in either order; the client matches each to its request solely by `id`. Here the response to `id` 3 arrives before the response to `id` 2:

```json
{ "jsonrpc": "2.0", "id": 3, "result": { "resultType": "complete", "resources": [] } }
```

```json
{ "jsonrpc": "2.0", "id": 2, "result": { "resultType": "complete", "tools": [] } }
```

If the transport connection is lost after these requests are sent but before their responses arrive, requests `id` 2 and `id` 3 MUST be considered failed (see §7.5 Error and Disconnection Handling), and the client MAY retry them on a new connection because no state is bound to the lost connection (see §7.6 Character Encoding and Statelessness).

## §8 The stdio Transport

This section defines the **stdio** transport: a binding that carries Model Context Protocol messages over the standard input and standard output streams of a client-launched subprocess. The protocol semantics are identical to every other transport; this binding defines only how messages are framed and delivered, how request metadata is carried, and how cancellation and process lifecycle are signaled. For message types and JSON-RPC structure see §3 Base Message Format; for the per-request envelope see §4 Request Metadata and the Stateless Model; for discovery see §5 Protocol Revision, Version Negotiation, and Discovery; for error codes see §22 Error Handling and Error Codes. [MCP][JSONRPC2]

### §8.1 Transport Model

In the stdio transport, the client launches the server as a subprocess (child process). The two ends communicate over the subprocess's standard streams: [MCP]

- The client writes messages to the server's standard input (`stdin`) and reads messages from the server's standard output (`stdout`).
- Equivalently, from the server's perspective: the server reads messages from its `stdin` and writes messages to its `stdout`.
- The server's standard error (`stderr`) stream carries free-form diagnostic text and is NOT part of the protocol (see §8.4 Standard Error).

A single subprocess carries exactly one bidirectional protocol connection. All messages in each direction share that one channel; there are no per-request streams and no other communication channels defined by this binding.

This binding depends on the standard streams only for its process-lifecycle rules (see §8.6 Subprocess Lifecycle). The wire format — one newline-delimited JSON-RPC message per line over a reliable bidirectional byte stream — is otherwise self-contained. A custom transport that runs over a reliable bidirectional byte stream (for example a Unix domain socket or a TCP connection) SHOULD reuse the framing and message rules of this section and supply channel-specific equivalents only for the subprocess-specific aspects (launch, `stderr`, shutdown by closing the stream, and process restart). [MCP]

### §8.2 Message Framing

Every JSON-RPC message exchanged on this transport is a single JSON-RPC request, notification, or response object encoded as JSON, serialized to a single line of UTF-8 text, and terminated by a newline. [MCP][RFC8259][JSONRPC2]

The framing rules are:

- **Encoding.** Messages MUST be UTF-8 encoded. [MCP][RFC8259]
- **One message per line.** Each message occupies exactly one line. Messages are delimited by newlines: the receiver splits the byte stream into messages at newline boundaries, and each resulting line (with the terminating newline removed) is exactly one complete JSON-RPC message to be parsed as JSON.
- **No embedded newlines.** A message MUST NOT contain embedded newlines. Because a newline is the message delimiter, any newline within the serialized JSON would be interpreted as a message boundary and corrupt framing. Senders MUST therefore serialize each JSON message such that it contains no literal newline characters. A JSON serializer that emits compact (non-pretty-printed) output, or that escapes any newline characters appearing inside string values as the two-character escape sequence `\n`, satisfies this requirement. [MCP][RFC8259]
- **Line terminator.** The sender MUST terminate each message with a single newline. Receivers SHOULD accept the line feed character (U+000A, byte `0x0A`, written `\n`) as the terminator and SHOULD tolerate an optional preceding carriage return (U+000D, byte `0x0D`, written `\r`) so that a `\r\n` sequence is also accepted; the carriage return, if present, is not part of the message and MUST be stripped before parsing.
- **Whitespace.** A line that is empty or contains only whitespace is not a JSON-RPC message; receivers SHOULD ignore such a line rather than treat it as malformed.

A "newline" for the purpose of delimiting messages is the line feed character (U+000A). The terms "line" and "message" are interchangeable on this transport: one line of `stdin`/`stdout` text corresponds to one JSON-RPC message.

### §8.3 Direction of Messages

Per the message patterns of the protocol, only two message directions exist on this transport (see §3 Base Message Format):

- The client sends JSON-RPC **requests** and **notifications** to the server by writing them to the server's `stdin`, one message per line. The client MUST NOT write JSON-RPC **responses** to `stdin`. [MCP][JSONRPC2]
- The server sends JSON-RPC **responses** and **notifications** to the client by writing them to its `stdout`, one message per line. The server MUST NOT write JSON-RPC **requests** to `stdout`. [MCP][JSONRPC2]

The server writes three kinds of messages to `stdout`:

1. **Responses** to client requests, correlated to the originating request by the JSON-RPC `id` field (see §3 Base Message Format).
2. **Notifications** that relate to an in-flight request, such as `notifications/progress` and `notifications/message` (see §15 Utilities).
3. **Notifications** delivered for an active `subscriptions/listen` request. The client MUST correlate each such notification using the `io.modelcontextprotocol/subscriptionId` field carried in the notification's `_meta` object (see §4 Request Metadata and the Stateless Model and §10 Server-to-Client Streaming and Subscriptions). [MCP][SEP-2575]

Because the server MUST NOT initiate JSON-RPC requests, any server-to-client interaction that requires a reply from the client during the processing of a client request is carried inside the server's response to that request, not as a separate `stdout` request (see §3 Base Message Format and §11 Multi-Round-Trip Requests). [MCP][SEP-2322][SEP-2260]

To cancel an in-flight request, the client MUST send a `notifications/cancelled` notification referencing the target request's `id`. Because stdio is a single shared bidirectional channel, there is no per-request stream to close. After cancellation, the server SHOULD stop work on the cancelled request as soon as practical and MUST NOT send any further messages (responses or related notifications) for that request (see §15 Utilities). [MCP]

### §8.4 Standard Error

The server MAY write UTF-8 text to its `stderr` stream for any logging purpose, including informational, debug, and error messages. Such text is not part of the protocol and MUST NOT be parsed as protocol messages. [MCP]

The client MAY capture, forward, or ignore the server's `stderr` output. The client MUST NOT interpret `stderr` content as JSON-RPC messages, and SHOULD NOT assume that the presence of `stderr` output indicates an error condition. [MCP]

Protocol log messages that are intended for the client as data (rather than free-form diagnostics) are delivered as `notifications/message` notifications on `stdout`, not on `stderr` (see §4 Request Metadata and the Stateless Model and §15 Utilities). [MCP]

### §8.5 What MUST NOT Appear on the Standard Streams, and Malformed Lines

The server MUST NOT write anything to its `stdout` that is not a valid MCP message. Diagnostic text, progress banners, prompts, or any other non-message output MUST go to `stderr` (or be suppressed), never to `stdout`. Mixing such output into `stdout` corrupts the message stream. [MCP]

The client MUST NOT write anything to the server's `stdin` that is not a valid MCP message. [MCP]

A receiver (the client reading `stdout`, or the server reading `stdin`) handles input as follows:

- A line that is empty or whitespace-only is ignored (see §8.2 Message Framing).
- A non-empty, non-whitespace line that is not well-formed JSON, or that is well-formed JSON but is not a valid JSON-RPC message object, is a **malformed line**. The receiver SHOULD NOT crash or terminate the connection on a malformed line. It SHOULD treat the line as a transport-level error: discard the malformed line, MAY record a diagnostic (the server via `stderr`; the client via its own logging), and continue reading subsequent lines.
- If a malformed inbound message can be parsed far enough to recover a JSON-RPC request `id` and is recognizable as a request, the receiver of that request MAY return a JSON-RPC error response with code `-32700` (Parse error) or `-32600` (Invalid Request) as appropriate; if no `id` can be recovered, no response is sent and the line is silently discarded after optional diagnostic logging (see §22 Error Handling and Error Codes). [JSONRPC2]

Because framing is line-based, a single malformed line does not desynchronize the stream: the next newline still begins the next message. Receivers MUST resynchronize at the next newline rather than abandoning the connection.

### §8.6 Subprocess Lifecycle

#### §8.6.1 Startup and Absence of a Connection Handshake

The client establishes the connection by launching the server subprocess and connecting to its standard streams. No separate transport-level connection handshake, registration step, or session-establishment exchange occurs on this transport: the connection exists as soon as the subprocess is running with its streams wired up, and the server is stateless across requests (see §4 Request Metadata and the Stateless Model). [MCP][SEP-2575][SEP-2567]

There is no session identifier and no session lifetime. Each request stands alone and MUST carry its full per-request envelope in `_meta` (see §4 Request Metadata and the Stateless Model). The first message the client sends MAY be any request carrying the §4 envelope (for example a `tools/call` or `tools/list` request), or a `server/discover` request (see §5 Protocol Revision, Version Negotiation, and Discovery). There is no required first message and no required ordering beyond ordinary request/response correlation by `id`. [MCP][SEP-2575]

#### §8.6.2 Graceful Shutdown

The client SHOULD initiate shutdown by performing the following steps in order: [MCP]

1. Close the input stream to the server subprocess (that is, close the server's `stdin`). End-of-file on `stdin` is the primary and only portable graceful-shutdown signal.
2. Wait for the server process to exit.
3. If the server does not exit within a reasonable time, forcibly terminate the process (see §8.6.3 Forced Termination).

The server SHOULD exit promptly when its standard input is closed or reads from `stdin` return end-of-file. Honoring this signal reduces the need for forced termination. [MCP]

The server MAY initiate shutdown on its own by closing its `stdout` stream to the client and exiting. [MCP]

#### §8.6.3 Forced Termination

If the server does not exit within a reasonable time after `stdin` is closed, the client forcibly terminates the process using the mechanism appropriate for the operating system. On POSIX systems, forced termination typically escalates from `SIGTERM` to `SIGKILL`. On systems where POSIX signals are not available, clients use the platform-appropriate equivalent for terminating a process or its process group (for example a terminate-process call or a job-object kill). [MCP]

#### §8.6.4 Unexpected Termination and Restart

If the server process exits unexpectedly, the client SHOULD restart it. Because the protocol is stateless, any in-flight requests are simply lost and the client MAY retry them against the fresh process (see §4 Request Metadata and the Stateless Model). Any active `subscriptions/listen` streams MUST be re-established against the restarted process; they do not survive a process exit (see §10 Server-to-Client Streaming and Subscriptions). [MCP][SEP-2575]

### §8.7 Protocol-Revision Selection and Discovery

This transport has no header layer. All request metadata is carried inline in the JSON-RPC message body: every request MUST carry, in its `_meta` object, the protocol revision, the client identity, and the client's per-request capabilities (see §4 Request Metadata and the Stateless Model). The method name and arguments are carried where JSON-RPC places them. [MCP][SEP-2575]

Protocol-revision selection on stdio therefore works entirely through the §4 envelope. Each request declares the revision it uses in the `_meta` key `io.modelcontextprotocol/protocolVersion` (a required `string` whose value is a protocol-revision identifier such as the literal `"2026-07-28"`). There is no transport header to mirror this value into; the body is the sole source of truth. If the server does not support the requested revision, it returns an Unsupported Protocol Version error with numeric code `-32004` (see §22 Error Handling and Error Codes). [MCP][SEP-2575]

A client MAY probe the server's supported revisions by sending a `server/discover` request before sending any other request (see §5 Protocol Revision, Version Negotiation, and Discovery). The probe carries the client's preferred revision in `_meta`. There are three possible outcomes:

- The server returns a `DiscoverResult` whose `supportedVersions` field is a `string[]` of protocol-revision identifiers. The client selects a mutually supported revision from that list and continues, placing the selected value in `io.modelcontextprotocol/protocolVersion` on subsequent requests.
- The server returns the Unsupported Protocol Version error (code `-32004`). The client selects one of the revisions advertised in that error's data and continues. The client MUST NOT, on this outcome, fall back to any session-establishing handshake.
- The server returns any other JSON-RPC error, or does not respond within a reasonable timeout. A client that supports a handshake-based counterpart MAY treat this outcome as indicating such a counterpart and fall back to its handshake. This fallback MUST NOT be keyed to one specific error code, because such counterparts respond to an unrecognized pre-handshake request with implementation-defined errors (commonly `-32601` "Method not found" or `-32602` "Invalid params", per §22 Error Handling and Error Codes) or do not respond at all.

A client that supports only the current revision is not required to probe, but probing with `server/discover` before any other request is RECOMMENDED: it yields a deterministic capability answer and a deterministic failure when the counterpart cannot serve the request. [MCP][SEP-2575]

### §8.8 Example Exchange

The following shows one complete request/response exchange on stdio. The client writes a single request line to the server's `stdin`; the server writes a single response line to its `stdout`. Each line is shown wrapped here for readability, but on the wire each is a single line of UTF-8 text terminated by a newline with no embedded newlines.

Client to server (one line written to the server's `stdin`):

```json
{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"get_weather","arguments":{"city":"Paris"},"_meta":{"io.modelcontextprotocol/protocolVersion":"2026-07-28","io.modelcontextprotocol/clientInfo":{"name":"ExampleClient","version":"1.0.0"},"io.modelcontextprotocol/clientCapabilities":{}}}}
```

Server to client (one line written to the server's `stdout`):

```json
{"jsonrpc":"2.0","id":1,"result":{"content":[{"type":"text","text":"It is 18°C and sunny in Paris."}],"isError":false}}
```

Meanwhile, the server MAY emit free-form diagnostics to `stderr`, which are not protocol messages and are not parsed by the client (see §8.4 Standard Error):

```
[server] handling tools/call name=get_weather
```

A `server/discover` probe (see §8.7 Protocol-Revision Selection and Discovery) sent as the first message would appear on `stdin` as:

```json
{"jsonrpc":"2.0","id":0,"method":"server/discover","params":{"_meta":{"io.modelcontextprotocol/protocolVersion":"2026-07-28","io.modelcontextprotocol/clientInfo":{"name":"ExampleClient","version":"1.0.0"},"io.modelcontextprotocol/clientCapabilities":{}}}}
```

with a successful response line on `stdout`:

```json
{"jsonrpc":"2.0","id":0,"result":{"supportedVersions":["2026-07-28"]}}
```

## §9 The Streamable HTTP Transport

### §9.1 Overview and Scope

The **Streamable HTTP transport** carries Model Context Protocol messages over HTTP. A server operates as an independent process that handles multiple client connections. The server exposes a single HTTP endpoint — the **MCP endpoint** — identified by a URL (for example `https://example.com/mcp`). The MCP endpoint accepts HTTP POST requests. Each POST request body is exactly one JSON-RPC *request* or one JSON-RPC *notification*, encoded as defined in §3 Base Message Format. JSON-RPC messages MUST be UTF-8 encoded [RFC8259]. [MCP][SEP-2575]

The transport is the recommended binding for remote servers. It defines only how messages are framed, delivered, routed, and terminated; it does not change protocol semantics, which are identical on every transport. The per-request metadata model of §4 Request Metadata and the Stateless Model applies unchanged: the message body is the single source of truth. [MCP][SEP-2575]

Within this transport:

- The client sends every JSON-RPC request or notification as its own HTTP POST.
- The server answers each POST that carries a request with either a single JSON object or a server-sent event stream scoped to that request [HTML-SSE].
- Server-to-client interactions (sampling, elicitation, listing roots) are NOT delivered as separate server-initiated JSON-RPC requests. They are embedded as input requests inside a result and resolved by client retry, per the multi-round-trip mechanism (see §11 Multi-Round-Trip Requests) [SEP-2322][SEP-2260].
- Long-lived change notifications (such as list-changed notifications and resource-updated notifications) are delivered on the response stream of a `subscriptions/listen` request (see §10 Server-to-Client Streaming and Subscriptions), not on a separate long-lived endpoint [SEP-2575].

A client implementing this transport MUST support both response shapes defined in §9.6 Response Shapes. [MCP]

### §9.2 The Client POST Request

Every JSON-RPC message a client sends MUST be a new HTTP POST request to the MCP endpoint. [MCP][SEP-2575]

The following rules apply to each POST:

1. The client MUST use the HTTP method `POST`.
2. The request body MUST be exactly one JSON-RPC *request* or one JSON-RPC *notification* (§3 Base Message Format). A client MUST NOT send a JSON-RPC *response* to the server; servers do not initiate JSON-RPC requests, so there is no response for a client to send.
3. The body MUST NOT be a batch (an array of messages); each message is its own POST.
4. The client MUST include the request headers REQUIRED by §9.3 Required Request Headers and the routing headers REQUIRED by §9.4 Routing Headers.

When the body is a JSON-RPC *notification*:

- If the server accepts the notification, the server MUST respond with HTTP status `202 Accepted` and no response body.
- If the server cannot accept the notification, the server MUST respond with an HTTP error status (for example `400 Bad Request`). The HTTP response body MAY contain a JSON-RPC error response whose `id` is omitted.

When the body is a JSON-RPC *request*, the server MUST respond with one of the two response shapes defined in §9.6 Response Shapes. [MCP]

### §9.3 Required Request Headers

Each POST MUST carry the following HTTP request headers. Header field names are case-insensitive; senders and receivers MUST compare field names case-insensitively. Header field *values* that mirror body fields (such as method names and protocol-version strings) are case-sensitive and MUST be compared exactly [RFC9110]. [MCP][SEP-2575]

#### §9.3.1 `Content-Type`

REQUIRED. The value MUST be `application/json`. The body is a single JSON-RPC message encoded as JSON [RFC8259].

```http
Content-Type: application/json
```

#### §9.3.2 `Accept`

REQUIRED. The client MUST indicate that it can accept both a single JSON response and an event stream. The `Accept` header MUST list both of the following media types:

- `application/json`
- `text/event-stream`

```http
Accept: application/json, text/event-stream
```

A client that does not advertise both media types is non-conforming, because it cannot accept whichever response shape the server selects per §9.6 Response Shapes. [MCP]

#### §9.3.3 `MCP-Protocol-Version`

REQUIRED. This header carries the protocol-revision string. Its value MUST equal the `io.modelcontextprotocol/protocolVersion` field carried in the request body's `_meta` object (§4 Request Metadata and the Stateless Model).

```http
MCP-Protocol-Version: 2026-07-28
```

Rejection rules:

- If the `MCP-Protocol-Version` header is absent, a server that does not support clients of revisions earlier than the one that introduced this header MUST reject the request with HTTP `400 Bad Request` and a JSON-RPC error response with code `-32001` (`HeaderMismatch`; see §9.8 The `-32001` `HeaderMismatch` Error). A server that supports such earlier clients MAY instead treat a request that omits the header as the earliest revision that did not define it, rather than rejecting it [MCP][SEP-2575].
- If the `MCP-Protocol-Version` header value does not equal the `io.modelcontextprotocol/protocolVersion` field in the body `_meta`, the server MUST reject the request with HTTP `400 Bad Request` and a JSON-RPC error response with code `-32001` (`HeaderMismatch`; see §9.8 The `-32001` `HeaderMismatch` Error).
- If the server does not implement the requested protocol version — whether the version is unknown to the server, or is a version the server has chosen not to support — the server MUST reject the request with HTTP `400 Bad Request` and an `UnsupportedProtocolVersionError` (JSON-RPC error code `-32004`) whose `error.data.supported` lists the protocol versions the server supports and whose `error.data.requested` echoes the requested version (see §5 Protocol Revision, Version Negotiation, and Discovery and §22 Error Handling and Error Codes).

[MCP][SEP-2575]

### §9.4 Routing Headers

The transport mirrors selected JSON-RPC body fields into HTTP headers so that intermediaries (load balancers, gateways, observability tooling) can route and inspect requests without parsing the request body. The body remains the single source of truth; this section and §9.5 Tool Parameters Surfaced as Headers define how mismatches are rejected. [SEP-2243][SEP-2575]

The following headers carry routing information. They are REQUIRED for compliance as indicated.

| Header field name | Mirrored body source | REQUIRED on |
| --- | --- | --- |
| `Mcp-Method` | `method` | every POST (requests and notifications) |
| `Mcp-Name` | `params.name`, or `params.uri` for `resources/read` | `tools/call`, `resources/read`, and `prompts/get` requests |

#### §9.4.1 `Mcp-Method`

REQUIRED on every POST. The value MUST equal the JSON-RPC `method` string in the body verbatim and case-sensitively (for example `tools/call`).

```http
Mcp-Method: tools/call
```

#### §9.4.2 `Mcp-Name`

REQUIRED on `tools/call`, `resources/read`, and `prompts/get` requests. The value carries the primary name targeted by the method:

- For `tools/call`: the value MUST equal `params.name` (the tool name).
- For `prompts/get`: the value MUST equal `params.name` (the prompt name).
- For `resources/read`: the value MUST equal `params.uri` (the resource URI).

```http
Mcp-Name: get_weather
```

The header MUST NOT be sent for methods that do not define a targeted name or URI.

#### §9.4.3 Routing-Header Mismatch

Servers that process the request body MUST reject any request where a routing-header value does not match the corresponding value in the request body, and any request that omits a REQUIRED routing header. The server MUST respond with HTTP `400 Bad Request` and a JSON-RPC error response with code `-32001` (`HeaderMismatch`; see §9.8 The `-32001` `HeaderMismatch` Error). This prevents security vulnerabilities that arise when one component routes on the header value while another component executes on the body value. [SEP-2243][SEP-2575]

### §9.5 Tool Parameters Surfaced as Headers

A server MAY designate specific tool parameters to be mirrored into HTTP request headers for routing and observability. The mechanism is OPTIONAL for servers to use; clients using this transport MUST support it. [SEP-2243]

#### §9.5.1 The `x-mcp-header` Annotation

A server declares that a tool parameter is surfaced as a header by adding an `x-mcp-header` extension property to that parameter's schema within the tool's `inputSchema` (see §16 Tools). The value of `x-mcp-header` is a string — the *name portion* used to construct the request header name `Mcp-Param-{name}` (see §9.5.2 Header Name Construction and Client Emission).

Constraints on each `x-mcp-header` value:

- MUST NOT be empty.
- MUST match the HTTP field-name token syntax: one or more `tchar` characters (`1*tchar`) [RFC9110].
- MUST NOT contain control characters, including carriage return (CR, byte `0x0D`) or line feed (LF, byte `0x0A`).
- MUST be case-insensitively unique among all `x-mcp-header` values within the same `inputSchema`.
- MUST be applied only to parameters whose JSON type is a primitive: `integer`, `string`, or `boolean`. Parameters of JSON type `number` MUST NOT carry the annotation. Integer values MUST lie within the range −(2^53)+1 to (2^53)−1 inclusive.
- MAY be applied to properties at any nesting depth within `inputSchema`, not only top-level properties.

A client using this transport MUST reject any tool definition in which an `x-mcp-header` value violates these constraints. Rejection means the client MUST exclude the invalid tool from the result it returns for `tools/list` (see §16 Tools); a single malformed tool definition MUST NOT prevent other valid tools from being used. The client SHOULD log a warning identifying the rejected tool name and the reason. Clients using other transports (for example stdio, §8 The stdio Transport) MAY ignore `x-mcp-header` annotations entirely. [SEP-2243]

Example tool definition declaring one parameter as a header:

```json
{
  "name": "execute_sql",
  "description": "Execute SQL on a managed database",
  "inputSchema": {
    "type": "object",
    "properties": {
      "region": {
        "type": "string",
        "description": "The region to execute the query in",
        "x-mcp-header": "Region"
      },
      "query": {
        "type": "string",
        "description": "The SQL query to execute"
      }
    },
    "required": ["region", "query"]
  }
}
```

#### §9.5.2 Header Name Construction and Client Emission

For each annotated parameter that is present in `params.arguments`, the client constructs a header field whose name is `Mcp-Param-` immediately followed by the `x-mcp-header` value (for example `Mcp-Param-Region`).

When constructing a `tools/call` POST, the client MUST:

1. Append the `Mcp-Method` header and, where applicable, the `Mcp-Name` header (§9.4 Routing Headers).
2. Inspect the tool's `inputSchema` for properties annotated with `x-mcp-header` and read the value of each such parameter from `params.arguments`.
3. Encode each value per §9.5.3 Value Encoding.
4. Append one `Mcp-Param-{name}: {encoded-value}` header per annotated parameter present in the arguments.

Emission rules per parameter:

- If the parameter value is provided in `params.arguments`, the client MUST include the corresponding header, and the server MUST validate that it matches the body (§9.5.4 Receiver Validation of Parameter Headers).
- If the parameter value is `null`, the client MUST omit the header, and the server MUST NOT expect it.
- If the parameter is absent from `params.arguments`, the client MUST omit the header, and the server MUST NOT expect it.
- A client that omits a header while the value is present in the body is non-conforming, and the server MUST reject such a request (§9.5.4 Receiver Validation of Parameter Headers).

If the client does not have the tool's `inputSchema` (for example, `tools/list` has not yet been called), or the cached schema is stale, the client SHOULD send the request without custom `Mcp-Param-*` headers. If the server then rejects the request because required custom headers are missing, the client SHOULD call `tools/list` to obtain the current `inputSchema` and retry the original request with the appropriate headers. A client MAY pre-load tool definitions by other means to enable header emission without a prior `tools/list` call. [SEP-2243]

#### §9.5.3 Value Encoding

A client MUST encode each parameter value before placing it in a header, to ensure safe transmission and prevent injection. [SEP-2243]

First, convert the parameter value to its string representation by JSON type:

- `string`: use the value as-is.
- `integer`: convert to its decimal string representation (for example `42`, `-7`).
- `boolean`: convert to the lowercase literal `true` or `false`.

HTTP header field values consist of visible ASCII characters (bytes `0x21`–`0x7E`), space (`0x20`), and horizontal tab (`0x09`) [RFC9110]. When the string representation cannot be safely carried as a plain ASCII header value — for example it contains non-ASCII characters, contains control characters, or has leading or trailing whitespace — the client MUST Base64-encode the UTF-8 bytes of the string [RFC4648] and wrap the result in the sentinel form:

```text
Mcp-Param-{name}: =?base64?{Base64EncodedValue}?=
```

The prefix `=?base64?` and the suffix `?=` are case-sensitive and MUST appear exactly as shown (lowercase). A receiver that needs to inspect the value MUST detect this sentinel and decode the Base64 payload accordingly. To avoid ambiguity, a client MUST also Base64-encode (and wrap in the sentinel) any plain-ASCII value that itself begins with `=?base64?` and ends with `?=`. [SEP-2243][RFC4648]

Encoding examples:

| Original value | Reason | Encoded header |
| --- | --- | --- |
| `us-west1` | plain ASCII | `Mcp-Param-Region: us-west1` |
| `Hello, 世界` | contains non-ASCII | `Mcp-Param-Greeting: =?base64?SGVsbG8sIOS4lueVjA==?=` |
| ` padded ` | leading/trailing spaces | `Mcp-Param-Text: =?base64?IHBhZGRlZCA=?=` |
| `line1\nline2` | contains newline | `Mcp-Param-Text: =?base64?bGluZTEKbGluZTI=?=` |
| `=?base64?literal?=` | matches sentinel pattern | `Mcp-Param-Val: =?base64?PT9iYXNlNjQ/bGl0ZXJhbD89?=` |

#### §9.5.4 Receiver Validation of Parameter Headers

An intermediary that does not recognize an `Mcp-Param-{name}` header MUST forward it and otherwise ignore it [RFC9110].

Any receiver that processes the message body MUST:

- Reject a request whose recognized `Mcp-Param-{name}` header contains characters not permitted in a header value (per §9.5.3 Value Encoding), responding with HTTP `400 Bad Request` and JSON-RPC error code `-32001` (`HeaderMismatch`).
- Decode the header value (decoding the Base64 payload first when the sentinel form is present) and verify that it matches the corresponding value in the request body. If any value does not match, the receiver MUST reject the request with HTTP `400 Bad Request` and JSON-RPC error code `-32001` (`HeaderMismatch`).

When validating an integer parameter, a receiver SHOULD compare the header value and the body value numerically rather than as strings (for example `42.0` and `42` are equal). [SEP-2243][SEP-2575]

### §9.6 Response Shapes

When the POST body is a JSON-RPC *request*, the server MUST select exactly one of two response shapes. The server chooses per request. Successful HTTP delivery of either shape uses HTTP status `200 OK`. [MCP][SEP-2575]

#### §9.6.1 Single JSON Response

The server responds with HTTP `200 OK`, `Content-Type: application/json`, and a body containing exactly one JSON-RPC *response* (a result response or an error response per §3 Base Message Format) whose `id` equals the request `id`.

This shape is used when the server produces the response without sending any request-scoped notifications.

```http
HTTP/1.1 200 OK
Content-Type: application/json

{ "jsonrpc": "2.0", "id": 1, "result": { } }
```

#### §9.6.2 Event-Stream Response

The server responds with HTTP `200 OK` and `Content-Type: text/event-stream`, opening a server-sent event stream scoped to this single request [HTML-SSE]. This shape is used when the server emits request-scoped notifications (for example progress or logging, see §15 Utilities) before the final response.

Stream framing rules:

- The stream is encoded in the `text/event-stream` format: each event is a sequence of lines; a `data:` field carries one line of the event payload; an event is terminated by a blank line (a line containing only a line feed) [HTML-SSE].
- Each event's `data` field carries exactly one JSON-RPC message serialized as JSON [RFC8259].
- The server MAY send JSON-RPC *notifications* on the stream before the final response. Every such notification MUST relate to the originating request (for example a `notifications/progress` that references the request's `progressToken`, or a `notifications/message` log entry for this request).
- The server MUST NOT send independent JSON-RPC *requests* on this stream. Server-to-client interactions are instead embedded in a result as input requests and resolved by client retry (§11 Multi-Round-Trip Requests).
- The final JSON-RPC *response* to the originating request SHOULD terminate the stream. After delivering the final response, the server MUST NOT send any further messages for that request.

When initiating an event stream, the server SHOULD include the response header `X-Accel-Buffering: no` so that reverse proxies do not buffer events and deliver them immediately.

Resumable event streams are not supported; a `Last-Event-ID` request header has no effect and MUST be ignored.

Closing the event-stream response stream MUST be treated by the server as cancellation of that request. Because each request has its own response stream, the disconnect is unambiguous: the server SHOULD stop work on the cancelled request as soon as practical and MUST NOT send any further messages for it (see §15 Utilities for cancellation semantics). [MCP][SEP-2575]

### §9.7 HTTP Status-Code Mapping

Status codes map to protocol and transport conditions as follows [RFC9110]. The full JSON-RPC error-code registry is defined in §22 Error Handling and Error Codes; this section maps those conditions onto HTTP.

| Condition | HTTP status | Body |
| --- | --- | --- |
| Request handled successfully (single JSON or event stream) | `200 OK` | JSON-RPC response (§9.6 Response Shapes) |
| Notification accepted | `202 Accepted` | empty |
| `MCP-Protocol-Version`, `Mcp-Method`, or `Mcp-Name` missing | `400 Bad Request` | JSON-RPC error `-32001` (`HeaderMismatch`) |
| A routing or parameter header value disagrees with the body, or a parameter header is malformed | `400 Bad Request` | JSON-RPC error `-32001` (`HeaderMismatch`) |
| Requested protocol version unsupported (§5 Protocol Revision, Version Negotiation, and Discovery) | `400 Bad Request` | `UnsupportedProtocolVersionError`, code `-32004` |
| Required client capability not declared (§5 Protocol Revision, Version Negotiation, and Discovery) | `400 Bad Request` | `MissingRequiredClientCapabilityError`, code `-32003` |
| Parameter-validation error (§22 Error Handling and Error Codes) | `400 Bad Request` | JSON-RPC error `-32602` (`Invalid params`) |
| Malformed JSON body (§22 Error Handling and Error Codes) | `400 Bad Request` | JSON-RPC error `-32700` (`Parse error`) |
| Body is not a valid JSON-RPC request object (§22 Error Handling and Error Codes) | `400 Bad Request` | JSON-RPC error `-32600` (`Invalid request`) |
| Requested RPC method not implemented by the server (§22 Error Handling and Error Codes) | `404 Not Found` | JSON-RPC error `-32601` (`Method not found`) |
| `Origin` header present and invalid | `403 Forbidden` | MAY contain a JSON-RPC error response with no `id` |

A JSON-RPC error response delivered with a `400 Bad Request` carries the standard error object shape (see §3 Base Message Format and §22 Error Handling and Error Codes):

```ts
interface JSONRPCErrorResponse {
  jsonrpc: "2.0";
  id?: RequestId;   // omitted when no request id can be determined
  error: Error;     // the canonical error object (§3.8)
}
```

The `404 Not Found` for an unknown method always carries a JSON-RPC error body with code `-32601`; this distinguishes an MCP endpoint from a `404` returned by a host that does not serve the MCP endpoint at all. [MCP][SEP-2575][JSONRPC2]

### §9.8 The `-32001` `HeaderMismatch` Error

A receiver that rejects a request because the HTTP headers do not match the request body, or because a REQUIRED header is missing or malformed, MUST return HTTP `400 Bad Request` and a JSON-RPC error response with the following code, which lies in the implementation-defined server-error range `-32000` to `-32099` [JSONRPC2]:

| Code | Name | Meaning |
| --- | --- | --- |
| `-32001` | `HeaderMismatch` | The HTTP headers do not match the corresponding values in the request body, or a REQUIRED header is missing or malformed. |

The conditions that MUST produce `-32001` are:

- A REQUIRED standard header (`MCP-Protocol-Version`, `Mcp-Method`, or `Mcp-Name`) is missing.
- Any header value (routing header per §9.4 Routing Headers, protocol-version header per §9.3.3 `MCP-Protocol-Version`, or `Mcp-Param-*` per §9.5 Tool Parameters Surfaced as Headers) does not match the corresponding request-body value.
- An `Mcp-Param-*` header value contains invalid characters.

An intermediary that enforces policy by inspecting mirrored headers MUST return an appropriate HTTP error status (for example `400 Bad Request`) on a validation failure but is not REQUIRED to return a JSON-RPC error body. Such an intermediary SHOULD verify that the `MCP-Protocol-Version` header indicates a version that requires header-body validation before trusting any mirrored header; if the header is absent, the intermediary SHOULD reject the request rather than trust unvalidated headers.

Example `-32001` error response:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "error": {
    "code": -32001,
    "message": "Header mismatch: Mcp-Name header value 'foo' does not match body value 'bar'"
  }
}
```

[SEP-2243][SEP-2575][JSONRPC2]

### §9.9 Statelessness at the HTTP Layer

This transport is stateless, consistent with the per-request model of §4 Request Metadata and the Stateless Model. The following rules are normative [SEP-2567][SEP-2575]:

- There is NO session-establishment request and NO session-bound state. The transport MUST NOT require any handshake to precede other requests.
- The transport MUST NOT use a session identifier header. A server MUST NOT mint, require, or echo a session identifier. If a client sends a session-identifier header, the server MUST ignore it.
- The transport MUST NOT require server affinity or sticky routing. The MCP endpoint MUST be able to serve any request on any server instance, because every request carries its complete protocol metadata in the body `_meta` (§4 Request Metadata and the Stateless Model) and mirrored routing headers (§9.4 Routing Headers).
- Server-to-client streaming for change notifications is obtained exclusively through the `subscriptions/listen` request (§10 Server-to-Client Streaming and Subscriptions): the response to that request is itself an event stream that stays open and delivers the change notifications the client opted in to (such as `notifications/tools/list_changed` and `notifications/resources/updated`). There is NO separate long-lived GET endpoint. Request-scoped notifications (progress, logging) flow only on the response stream of the request they relate to (§9.6.2 Event-Stream Response), never on the listen stream.
- The HTTP methods used for session lifecycle in other bindings are NOT part of this transport. A server that supports only this transport SHOULD respond with HTTP `405 Method Not Allowed` to an HTTP `GET` or `DELETE` directed at the MCP endpoint. A `Last-Event-ID` request header MUST be ignored; streams are not resumable.

### §9.10 Worked Examples

#### §9.10.1 POST with All Required Headers

A `tools/call` request carrying the required request headers (§9.3 Required Request Headers), routing headers (§9.4 Routing Headers), and one parameter header (§9.5 Tool Parameters Surfaced as Headers):

```http
POST /mcp HTTP/1.1
Host: example.com
Content-Type: application/json
Accept: application/json, text/event-stream
MCP-Protocol-Version: 2026-07-28
Mcp-Method: tools/call
Mcp-Name: execute_sql
Mcp-Param-Region: us-west1

{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "execute_sql",
    "arguments": {
      "region": "us-west1",
      "query": "SELECT * FROM users"
    },
    "_meta": {
      "io.modelcontextprotocol/protocolVersion": "2026-07-28",
      "io.modelcontextprotocol/clientInfo": {
        "name": "ExampleClient",
        "version": "1.0.0"
      },
      "io.modelcontextprotocol/clientCapabilities": {}
    }
  }
}
```

#### §9.10.2 Single JSON Response

The server resolves the request without any request-scoped notifications and replies with a single JSON object (§9.6.1 Single JSON Response):

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "content": [
      { "type": "text", "text": "42 rows returned." }
    ],
    "isError": false
  }
}
```

#### §9.10.3 Event-Stream Response with a Progress Notification

The same request answered with an event stream (§9.6.2 Event-Stream Response): the server emits one progress notification referencing the request's `progressToken`, then the final response, which terminates the stream. (The request carried `params._meta.progressToken` set to `"sql-1"`.)

```http
HTTP/1.1 200 OK
Content-Type: text/event-stream
X-Accel-Buffering: no

data: {"jsonrpc":"2.0","method":"notifications/progress","params":{"progressToken":"sql-1","progress":50,"total":100}}

data: {"jsonrpc":"2.0","method":"notifications/progress","params":{"progressToken":"sql-1","progress":100,"total":100}}

data: {"jsonrpc":"2.0","id":1,"result":{"content":[{"type":"text","text":"42 rows returned."}],"isError":false}}

```

Each event is one `data:` line carrying one JSON-RPC message, terminated by a blank line [HTML-SSE]. The final event carries the JSON-RPC *response* whose `id` equals the request `id`; after it, the server sends no further messages and the stream closes.

#### §9.10.4 Notification POST

A standalone JSON-RPC notification is acknowledged with `202 Accepted` and an empty body (§9.2 The Client POST Request):

```http
POST /mcp HTTP/1.1
Host: example.com
Content-Type: application/json
Accept: application/json, text/event-stream
MCP-Protocol-Version: 2026-07-28
Mcp-Method: notifications/progress

{
  "jsonrpc": "2.0",
  "method": "notifications/progress",
  "params": { "progressToken": "sql-1", "progress": 100, "total": 100 }
}
```

```http
HTTP/1.1 202 Accepted
```

### §9.11 Security and Endpoint Binding

A server MUST validate the `Origin` header on every incoming connection to prevent DNS-rebinding attacks, in which a remote web origin causes a victim's browser to direct requests at a locally reachable MCP endpoint [MCP]. If the `Origin` header is present and is not an origin the server is configured to accept, the server MUST reject the request with HTTP `403 Forbidden`; the HTTP response body MAY contain a JSON-RPC error response with no `id` (see §9.7 HTTP Status-Code Mapping). Without Origin validation, an attacker-controlled remote website could reach a local MCP server through the user's browser.

When a server runs locally, it SHOULD bind its MCP endpoint only to the loopback interface (`127.0.0.1`) rather than to all network interfaces (`0.0.0.0`), so that the endpoint is not reachable from other hosts on the network [MCP].

A server exposing a Streamable HTTP endpoint SHOULD implement proper authentication for all connections; where authorization is used, it MUST satisfy §23 Authorization [MCP].

### §9.12 Backward Compatibility

A client that supports both this revision and an earlier revision requiring an `initialize` handshake MAY detect which a server implements by attempting a modern POST first. On an HTTP `400 Bad Request`, the client SHOULD inspect the response body before falling back, because a modern server also returns `400` for `UnsupportedProtocolVersionError` (`-32004`), `MissingRequiredClientCapabilityError` (`-32003`), and `HeaderMismatch` (`-32001`) (see §9.7 HTTP Status-Code Mapping):

- If the body contains a recognized JSON-RPC error of this revision, the server speaks a modern revision; the client MUST retry using the versions advertised in `error.data.supported` or correct the request, and MUST NOT fall back to an `initialize` handshake.
- If the body is empty or is not a recognized JSON-RPC error of this revision, the client MAY fall back to `initialize` and continue with the earlier revision for subsequent requests [MCP][SEP-2575].

To interoperate with the deprecated HTTP+SSE transport, a server that wishes to support older clients SHOULD continue to host that transport's SSE and POST endpoints alongside the Streamable HTTP MCP endpoint. A client that wishes to support older servers SHOULD, after attempting a POST to the configured URL, fall back as follows: if the POST fails with HTTP `400 Bad Request`, `404 Not Found`, or `405 Method Not Allowed` AND the response body is not a recognized JSON-RPC error of this revision, the client SHOULD issue an HTTP `GET` to the URL; if that opens an SSE stream whose first event is an `endpoint` event, the client SHOULD treat the server as running the deprecated HTTP+SSE transport and use that transport for subsequent communication [MCP].

## §10 Server-to-Client Streaming and Subscriptions

This section defines the mechanism by which a client opts in to receive server-initiated change notifications over a single long-lived stream. The mechanism is the `subscriptions/listen` request: its response is a long-lived stream that carries only the notification kinds the client explicitly requested. The mechanism is uniform across transports (see §9 The Streamable HTTP Transport, §8 The stdio Transport); on Streamable HTTP the long-lived stream is the `text/event-stream` response body of the `subscriptions/listen` POST, and on stdio it is the sequence of messages tagged with the subscription identifier on the shared channel. [MCP][SEP-2575]

### §10.1 Subscription Model

A client that wants server-initiated change notifications MUST send a `subscriptions/listen` request. The server's response to that request is a single long-lived stream that stays open and delivers the notification kinds the client opted in to. There is exactly one such stream per `subscriptions/listen` request. [SEP-2575]

The following rules govern the model:

- A client MUST explicitly specify which notification kinds it wants (see §10.2 The `subscriptions/listen` Request and the Notification Filter). A server MUST NOT send a notification kind the client did not opt into on that subscription.
- The first message the server sends on the stream MUST be a `notifications/subscriptions/acknowledged` notification (see §10.3 Acknowledgement).
- Every notification delivered on the stream MUST carry the reserved metadata key `io.modelcontextprotocol/subscriptionId` (see §10.4 Subscription Correlation).
- There is NO separate long-lived GET endpoint, and there is NO event-id-based stream resumption. A client MUST NOT rely on a `Last-Event-ID` mechanism to resume a subscription; subscriptions are not resumable and MUST be re-established by issuing a new `subscriptions/listen` request (see §10.7 Subscription Lifecycle). [SEP-2575][SEP-2567]

A client MAY hold multiple active subscriptions concurrently — for example, one subscription listening for tools-list changes and another for resource updates. Each subscription is independent and is identified by the JSON-RPC `id` of its `subscriptions/listen` request (see §10.4 Subscription Correlation). [SEP-2575]

### §10.2 The `subscriptions/listen` Request and the Notification Filter

The request method name is the literal string `subscriptions/listen`. The request is a JSON-RPC request (it has an `id`; see §3 Base Message Format) and MUST carry a `params` object.

```ts
interface SubscriptionsListenRequest {
  jsonrpc: "2.0";
  id: string | number;          // the subscription identifier (see §10.4 Subscription Correlation)
  method: "subscriptions/listen";
  params: SubscriptionsListenRequestParams;
}

interface SubscriptionsListenRequestParams {
  // REQUIRED. The notification kinds the client opts in to on this stream.
  // The server MUST NOT send notification kinds not requested here.
  notifications: SubscriptionFilter;

  // OPTIONAL request metadata, per §4 Request Metadata and the Stateless Model.
  _meta?: { [key: string]: unknown };
}
```

The `notifications` field is REQUIRED and is typed as a `SubscriptionFilter`:

```ts
interface SubscriptionFilter {
  // If true, the client opts in to notifications/tools/list_changed.
  toolsListChanged?: boolean;

  // If true, the client opts in to notifications/prompts/list_changed.
  promptsListChanged?: boolean;

  // If true, the client opts in to notifications/resources/list_changed.
  resourcesListChanged?: boolean;

  // The client opts in to notifications/resources/updated for exactly
  // these resource URIs. Each entry is an absolute URI string.
  resourceSubscriptions?: string[];
}
```

Field-by-field constraints:

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `toolsListChanged` | `boolean` | OPTIONAL | When `true`, the client requests `notifications/tools/list_changed` on this stream when the tool list changes (see §16 Tools). |
| `promptsListChanged` | `boolean` | OPTIONAL | When `true`, the client requests `notifications/prompts/list_changed` on this stream when the prompt list changes (see §18 Prompts). |
| `resourcesListChanged` | `boolean` | OPTIONAL | When `true`, the client requests `notifications/resources/list_changed` on this stream when the resource list changes (see §17 Resources). |
| `resourceSubscriptions` | `string[]` | OPTIONAL | The set of resource URIs for which the client requests per-resource `notifications/resources/updated` notifications on this stream (see §17 Resources). Each element MUST be an absolute URI string [RFC3986]. An absent or empty array means no per-resource update subscriptions on this stream. |

All fields of `SubscriptionFilter` are OPTIONAL. Omitting a field (or setting a boolean field to `false`, or supplying an absent/empty `resourceSubscriptions`) is equivalent to not subscribing to that notification kind. A client SHOULD set at least one field; a `SubscriptionFilter` with no kinds requested yields a stream that carries only the acknowledgement (see §10.3 Acknowledgement) and no further notifications.

The `resourceSubscriptions` filter governs per-resource update notifications: the server MUST send `notifications/resources/updated` (see §10.5 Change Notifications on the Stream) ONLY for resources whose URIs the client listed in `resourceSubscriptions` on a `subscriptions/listen` request, and MUST NOT send `notifications/resources/updated` for a resource the client did not list. A server's support for per-resource update notifications is advertised by the `subscribe` feature of the `resources` server capability, and its support for `notifications/resources/list_changed` by the `listChanged` feature; capability negotiation is defined in §17 Resources. [MCP][SEP-2575]

### §10.3 Acknowledgement

The FIRST message the server sends on the `subscriptions/listen` stream MUST be a notification with the literal method name `notifications/subscriptions/acknowledged`. The server MUST send it before any change notification on that stream.

```ts
interface SubscriptionsAcknowledgedNotification {
  jsonrpc: "2.0";
  method: "notifications/subscriptions/acknowledged";
  params: SubscriptionsAcknowledgedNotificationParams;
}

interface SubscriptionsAcknowledgedNotificationParams {
  // REQUIRED. The subset of the requested filter the server agreed to honor.
  notifications: SubscriptionFilter;

  // The notification carries the subscription identifier in _meta
  // (see §10.4 Subscription Correlation); io.modelcontextprotocol/subscriptionId
  // is REQUIRED here.
  _meta?: { [key: string]: unknown };
}
```

The `notifications` field is REQUIRED and is a `SubscriptionFilter` (see §10.2 The `subscriptions/listen` Request and the Notification Filter). It reflects the subset of the requested filter that the server actually supports and agreed to honor. A notification kind the server does not support MUST be omitted from the acknowledged filter: for example, if the client requested `promptsListChanged: true` but the server has no prompts (does not support the prompts-list-changed kind), the server MUST omit `promptsListChanged` from the acknowledged `notifications`. Likewise, the server's acknowledged `resourceSubscriptions` reflects the subset of requested resource URIs for which it will deliver updates.

The acknowledgement notification MUST carry the subscription identifier in its `_meta` under the key `io.modelcontextprotocol/subscriptionId` (see §10.4 Subscription Correlation).

Clients SHOULD compare the acknowledged `notifications` filter against what they requested and handle any unsupported (omitted) kinds gracefully — for example, by not waiting indefinitely for a notification kind the server declined to honor.

### §10.4 Subscription Correlation

Every notification delivered for a subscription — including the `notifications/subscriptions/acknowledged` notification — MUST be tagged with the reserved metadata key `io.modelcontextprotocol/subscriptionId` inside the notification's `params._meta` object. The value is the subscription identifier as a string.

The subscription identifier is established as the JSON-RPC `id` of the `subscriptions/listen` request that opened the stream. The value carried in `io.modelcontextprotocol/subscriptionId` is that `id`, serialized as a JSON string (for example, request `"id": 1` yields `"io.modelcontextprotocol/subscriptionId": "1"`).

```ts
// Present in params._meta of every notification on a subscription stream.
{
  "io.modelcontextprotocol/subscriptionId": string;  // REQUIRED on subscription notifications
}
```

This key enables demultiplexing. On stdio (see §8 The stdio Transport), where all messages — request responses, request-scoped notifications, and the notifications for every active subscription — share a single channel, a client MUST use `io.modelcontextprotocol/subscriptionId` to correlate each incoming notification with the originating `subscriptions/listen` request, so that a client multiplexing several subscriptions over the one channel can route each notification to the correct subscription. On Streamable HTTP (see §9 The Streamable HTTP Transport), where each subscription has its own response stream, the key MUST still be present; clients MAY additionally rely on the per-stream separation. [SEP-2575][SEP-2243]

The reserved metadata key `io.modelcontextprotocol/subscriptionId` is case-sensitive and MUST be reproduced verbatim. Reserved `_meta` keys and their general rules are defined in §4 Request Metadata and the Stateless Model.

### §10.5 Change Notifications on the Stream

Exactly four notification kinds flow on a `subscriptions/listen` stream, each gated by the corresponding field of the requested `SubscriptionFilter` (see §10.2 The `subscriptions/listen` Request and the Notification Filter). Each is a JSON-RPC notification (no `id`) and each MUST carry `io.modelcontextprotocol/subscriptionId` in `params._meta` (see §10.4 Subscription Correlation).

**Tools list changed.** Method name `notifications/tools/list_changed`. Delivered when the set of available tools changes, if and only if the client requested `toolsListChanged: true`. Upon receipt the client SHOULD re-fetch the tool list (see §16 Tools). The notification shape is `ToolListChangedNotification`, defined in §16.8.

**Prompts list changed.** Method name `notifications/prompts/list_changed`. Delivered when the set of available prompts changes, if and only if the client requested `promptsListChanged: true`. Upon receipt the client SHOULD re-fetch the prompt list (see §18 Prompts). The notification shape is `PromptListChangedNotification`, defined in §18.6.

**Resources list changed.** Method name `notifications/resources/list_changed`. Delivered when the set of available resources changes, if and only if the client requested `resourcesListChanged: true`. Upon receipt the client SHOULD re-fetch the resource list (see §17 Resources). The notification shape is `ResourceListChangedNotification`, defined in §17.7.

**Resource updated.** Method name `notifications/resources/updated`. Delivered when a watched resource changes, if and only if the client listed that resource's URI (or a parent of it; see below) in `resourceSubscriptions`. This informs the client that the resource has changed and may need to be read again via `resources/read` (see §17 Resources). The notification shape is `ResourceUpdatedNotification` (with its `ResourceUpdatedNotificationParams`), defined in §17.7.

The `uri` field is REQUIRED and is an absolute URI string [RFC3986] identifying the resource that changed. The identified `uri` MAY be a sub-resource of a URI the client subscribed to: a client that subscribed to a container URI (for example, a directory) MAY receive `notifications/resources/updated` whose `uri` is a contained resource (for example, a file within that directory). The client correlates the notification to its subscription via `io.modelcontextprotocol/subscriptionId` (see §10.4 Subscription Correlation), not solely via the `uri`. [MCP]

A server MUST NOT send any of these four kinds unless the corresponding filter field was both requested by the client (see §10.2 The `subscriptions/listen` Request and the Notification Filter) and reflected in the acknowledged filter (see §10.3 Acknowledgement).

### §10.6 Boundary Between Subscription and Request-Scoped Notifications

There is a strict boundary between subscription notifications and request-scoped notifications. Request-scoped notifications are notifications that relate to a specific in-flight request — namely progress notifications (`notifications/progress`) and logging-message notifications (`notifications/message`) (see §15 Utilities).

- Request-scoped notifications MUST be delivered on the response stream of the specific request they relate to (see §9.6.2 Event-Stream Response). They MUST NOT be delivered on a `subscriptions/listen` stream.
- The four change-notification kinds of §10.5 Change Notifications on the Stream MUST be delivered on a `subscriptions/listen` stream. They MUST NOT be delivered on the response stream of an unrelated request.

Stated in both directions: a server MUST NOT send `notifications/progress` or `notifications/message` on a `subscriptions/listen` stream, and a server MUST NOT send `notifications/tools/list_changed`, `notifications/prompts/list_changed`, `notifications/resources/list_changed`, or `notifications/resources/updated` on the per-request response stream of a request other than `subscriptions/listen`. A client receiving a notification on the wrong stream SHOULD treat it as a protocol violation. [MCP][SEP-2575][SEP-2260]

### §10.7 Subscription Lifecycle

A subscription is bound to its stream and the underlying connection. The following lifecycle rules apply:

- **Client cancellation.** A client cancels a subscription by closing the stream. On Streamable HTTP (see §9 The Streamable HTTP Transport) the client closes the `text/event-stream` response of the `subscriptions/listen` POST; closing the stream cancels the subscription. On stdio (see §8 The stdio Transport) the client cancels by sending a `notifications/cancelled` notification referencing the `id` of the `subscriptions/listen` request (see §15 Utilities for cancellation).
- **Server teardown.** A server that tears down a subscription (for example, during shutdown) MUST signal teardown to the client: on Streamable HTTP it MUST close the `text/event-stream` response; on stdio it MUST send a `notifications/cancelled` notification referencing the `id` of the `subscriptions/listen` request.
- **Transport closure.** If the underlying transport closes (for example, an HTTP timeout, a TCP disconnect, or a stdio child-process exit), the subscription ends.
- **No retained state across connections.** The server MUST NOT retain subscription state across connections. When a connection is re-established (for example, after a stdio reconnect), the client MUST re-issue `subscriptions/listen` to re-establish each subscription it wants; the server holds no subscription state from the prior connection. [SEP-2575][SEP-2567]
- **No GET endpoint, no resumption.** There is no separate long-lived GET endpoint for server-initiated notifications, and there is no event-id-based resumption: a `Last-Event-ID` mechanism is not supported for subscription streams. A dropped subscription is re-established only by sending a new `subscriptions/listen` request, which yields a new subscription identifier (the `id` of the new request). [SEP-2575][SEP-2567][SEP-2243]

### §10.8 Examples

**A `subscriptions/listen` request.** The client opts in to tool-list changes and to per-resource updates for one resource. Its `id` (`1`) becomes the subscription identifier.

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "subscriptions/listen",
  "params": {
    "_meta": {
      "io.modelcontextprotocol/protocolVersion": "2026-07-28",
      "io.modelcontextprotocol/clientInfo": {
        "name": "ExampleClient",
        "version": "1.0.0"
      },
      "io.modelcontextprotocol/clientCapabilities": {}
    },
    "notifications": {
      "toolsListChanged": true,
      "resourceSubscriptions": ["file:///project/config.json"]
    }
  }
}
```

**The acknowledged notification as the first stream message.** The server supports both requested kinds and echoes them; the notification carries the subscription identifier `"1"`.

```json
{
  "jsonrpc": "2.0",
  "method": "notifications/subscriptions/acknowledged",
  "params": {
    "_meta": {
      "io.modelcontextprotocol/subscriptionId": "1"
    },
    "notifications": {
      "toolsListChanged": true,
      "resourceSubscriptions": ["file:///project/config.json"]
    }
  }
}
```

**A `resource-updated` notification carrying the subscription identifier.** Later on the same stream, the watched resource changes.

```json
{
  "jsonrpc": "2.0",
  "method": "notifications/resources/updated",
  "params": {
    "_meta": {
      "io.modelcontextprotocol/subscriptionId": "1"
    },
    "uri": "file:///project/config.json"
  }
}
```

**Over Streamable HTTP**, the request above is a single HTTP POST whose response is the long-lived event stream carrying the acknowledgement followed by change notifications:

```http
POST /mcp HTTP/1.1
Content-Type: application/json
Accept: application/json, text/event-stream
MCP-Protocol-Version: 2026-07-28
Mcp-Method: subscriptions/listen

{"jsonrpc":"2.0","id":1,"method":"subscriptions/listen","params":{"notifications":{"toolsListChanged":true,"resourceSubscriptions":["file:///project/config.json"]},"_meta":{"io.modelcontextprotocol/protocolVersion":"2026-07-28","io.modelcontextprotocol/clientInfo":{"name":"ExampleClient","version":"1.0.0"},"io.modelcontextprotocol/clientCapabilities":{}}}}
```

```http
HTTP/1.1 200 OK
Content-Type: text/event-stream
X-Accel-Buffering: no

data: {"jsonrpc":"2.0","method":"notifications/subscriptions/acknowledged","params":{"_meta":{"io.modelcontextprotocol/subscriptionId":"1"},"notifications":{"toolsListChanged":true,"resourceSubscriptions":["file:///project/config.json"]}}}

data: {"jsonrpc":"2.0","method":"notifications/resources/updated","params":{"_meta":{"io.modelcontextprotocol/subscriptionId":"1"},"uri":"file:///project/config.json"}}

```

The stream stays open and continues to carry the opted-in notifications until the client closes it or the server tears it down (see §10.7 Subscription Lifecycle).

# Part III — Interaction Patterns and Common Structures

## §11 Multi-Round-Trip Requests

This section defines the multi-round-trip request (MRTR) pattern: the single mechanism by which a server obtains additional information from a client while processing a client request [MCP][SEP-2322][SEP-2260]. It specifies the result that signals additional input is required, the opaque state token that carries continuation context, the retry parameters the client supplies, the full normative exchange algorithm including capability gating and load-shedding, the set of methods that participate, and complete wire examples. Result-type discrimination is defined in §3 Base Message Format; capabilities are defined in §6 Capabilities and Extensions; the full payloads of the individual input-request kinds are defined in §20 Elicitation and §21 Deprecated Client-Provided Capabilities; the missing-capability error is defined in §5 Protocol Revision, Version Negotiation, and Discovery.

### §11.1 The Multi-Round-Trip Request Pattern

When a server processing a client request needs information that only the client can supply — for example a structured value from the user, a list of filesystem roots, or a model completion — it does NOT open an independent solicitation to the client. Instead, the server completes the in-flight JSON-RPC response with a result whose `resultType` is the literal string `"input_required"`. That result carries a set of *input requests* describing what the server needs. The client fulfills those input requests locally, then **retries the original request** — same method, same original arguments — adding the gathered responses and an opaque continuation token. The server resumes its work and returns either a final result or a further `"input_required"` result, until the work is complete [SEP-2322][SEP-2260].

This pattern is the ONLY means by which a server solicits information from a client during request processing. The following are normative:

- A server **MUST NOT** send an independent JSON-RPC request to a client in order to gather input for an in-flight client request. All server-to-client solicitation **MUST** be expressed as an `"input_required"` result on the response to the request the server is processing [SEP-2322][SEP-2260].
- Because the server's input requests are conveyed inside a *result* (not a JSON-RPC request directed at the client), the server is NOT awaiting a JSON-RPC response to a server-issued request. The client's fulfillment is delivered by re-sending the original request, as a new JSON-RPC request with a new request `id`. The original request/response exchange that produced the `"input_required"` result is, from the JSON-RPC standpoint, already complete.
- A server that cannot make progress because the client declined, cancelled, or otherwise did not fulfill an input request handles that condition when it processes the retry (or its absence); it has no outstanding JSON-RPC request to fail. Senders **SHOULD NOT** assume a retry will always arrive or will always carry usable responses.

The pattern is stateless-friendly: the server need not retain in-memory state between the `"input_required"` response and the retry, because all continuation context can be encoded into the `requestState` token defined in §11.3 [SEP-2575][SEP-2322].

### §11.2 InputRequiredResult and the Input Requests

A server signals that additional input is required by returning a result object that extends the common `Result` shape (§3 Base Message Format) with `resultType` set to `"input_required"`.

```ts
interface InputRequiredResult {
  // From the common Result base (§3 Base Message Format).
  _meta?: { [key: string]: unknown };
  // MUST be the literal string "input_required".
  resultType: "input_required";

  // The requests the server needs the client to fulfill before the
  // original request can complete. Optional; see §11.5 (load-shedding).
  inputRequests?: InputRequests;

  // Opaque continuation token; see §11.3. Optional.
  requestState?: string;
}
```

Constraints on `InputRequiredResult`:

- `resultType` is REQUIRED and **MUST** equal the exact, case-sensitive string `"input_required"`.
- At least one of `inputRequests` or `requestState` **MUST** be present. A result that omits both is malformed and a client **MUST** treat it as an error (§22 Error Handling and Error Codes).
- `inputRequests` is OPTIONAL. When present it is a non-empty map (see `InputRequests` below). When absent, the result is a load-shedding signal (§11.5).
- `requestState` is OPTIONAL on the result but, in practice, REQUIRED whenever the server needs the retry to be associated with prior context (§11.3).

`InputRequests` is a map from a key to a single input-request object:

```ts
interface InputRequests {
  [key: string]: InputRequest;
}
```

- The keys are strings chosen by the server (the originator of the `InputRequiredResult`). Implementations MAY use any non-empty string key.
- Keys **MUST** be unique within the map. (As JSON object member names they are inherently unique [RFC8259]; a sender **MUST NOT** emit duplicate keys and a receiver encountering duplicate keys **MUST** treat the result as malformed.)
- The client uses these exact keys when returning responses (§11.4): each key it answers in `inputResponses` **MUST** be one of the keys present in `inputRequests`.

`InputRequest` is a discriminated union over the `method` field. The members are the input-request kinds a server may ask a client to fulfill:

```ts
type InputRequest =
  | CreateMessageRequest   // method: "sampling/createMessage"
  | ElicitRequest          // method: "elicitation/create"
  | ListRootsRequest;      // method: "roots/list"
```

Each member is discriminated by its `method` string and carries that kind's own `params`:

```ts
// Model-completion (sampling) input request.
// Full params are defined in §21 Deprecated Client-Provided Capabilities.
interface CreateMessageRequest {
  method: "sampling/createMessage";
  params: CreateMessageRequestParams;
}

// User-input (elicitation) input request.
// Full params (form and url modes) are defined in §20 Elicitation.
interface ElicitRequest {
  method: "elicitation/create";
  params: ElicitRequestParams;
}

// Filesystem-roots input request.
// Full params are defined in §21 Deprecated Client-Provided Capabilities.
interface ListRootsRequest {
  method: "roots/list";
  params?: { _meta: { [key: string]: unknown } };
}
```

The discriminator value (`method`) for each kind is exact and case-sensitive: `"sampling/createMessage"`, `"elicitation/create"`, `"roots/list"`. The complete payload of `ElicitRequestParams` (form-mode `requestedSchema` and url-mode `url`/`elicitationId`, the `mode` and `message` fields, and the `accept`/`decline`/`cancel` response model) is specified in §20 Elicitation. The complete payloads and result shapes of `sampling/createMessage` and `roots/list` are specified in §21 Deprecated Client-Provided Capabilities. The sampling and roots input-request kinds are deprecated client-provided capabilities; servers SHOULD prefer alternatives where available, and a server **MUST NOT** emit an input-request kind the client has not declared support for (§11.5, §6 Capabilities and Extensions).

A client **MUST** treat an `InputRequest` whose `method` is none of the three values above as an unrecognized input-request kind and **MUST** treat the `InputRequiredResult` as an error (§22 Error Handling and Error Codes) rather than guessing how to fulfill it.

### §11.3 requestState: The Opaque Continuation Token

`requestState` is a server-minted string that carries whatever the server needs in order to resume processing the original request on retry.

- `requestState` is OPAQUE to the client. The client **MUST** treat it as an uninterpreted blob: it **MUST NOT** parse, decode, validate the structure of, truncate, re-encode, or otherwise modify it.
- When the client retries the original request, it **MUST** echo the `requestState` value back verbatim, byte-for-byte identical to the value the server sent in the `InputRequiredResult`.
- `requestState` enables stateless continuation: a server MAY encode all continuation context (for example, the progress of its work, identifiers binding the input requests to a user, or references to externally stored state) into `requestState`, so that it need not retain server-side memory between the response and the retry [SEP-2575][SEP-2322]. A server MAY instead use `requestState` purely as an opaque handle to server-side state. The client cannot distinguish these strategies and **MUST NOT** try.
- The server is solely responsible for the integrity and confidentiality of `requestState`. Because the value round-trips through a potentially untrusted client, a server that encodes sensitive or trust-bearing context into `requestState` **MUST** protect it accordingly — for example, by authenticating it (so a tampered value is rejected) and encrypting it (so the client cannot read it). A server **MUST** validate `requestState` on the retry and **MUST** reject a value it did not mint or that has been altered, returning an error (§22 Error Handling and Error Codes). General confidentiality, tamper-resistance, and binding-to-user guidance for such tokens is in §28 Security Considerations.

### §11.4 The Retry Request: InputResponseRequestParams

To fulfill an `InputRequiredResult`, the client re-sends the **original request method with the original arguments**, augmented with two additional params: the gathered responses and the echoed continuation token. These additional params are described by `InputResponseRequestParams`, which any client-initiated request MAY carry:

```ts
interface InputResponseRequestParams {
  // From the common request params (§4 Request Metadata and the
  // Stateless Model): per-request metadata.
  _meta: { [key: string]: unknown };

  // The client's responses, keyed identically to the server's inputRequests.
  inputResponses?: InputResponses;

  // The requestState value echoed verbatim from the InputRequiredResult (§11.3).
  requestState?: string;
}
```

`InputResponses` is the response counterpart of `InputRequests`:

```ts
interface InputResponses {
  [key: string]: InputResponse;
}

// The result counterparts of the InputRequest kinds.
type InputResponse =
  | CreateMessageResult   // counterpart of "sampling/createMessage"
  | ElicitResult          // counterpart of "elicitation/create"
  | ListRootsResult;      // counterpart of "roots/list"
```

The member shapes (full definitions in §20 Elicitation and §21 Deprecated Client-Provided Capabilities) are:

```ts
// Counterpart of ElicitRequest. Full definition in §20 Elicitation.
interface ElicitResult {
  // "accept" — user submitted; "decline" — user explicitly declined;
  // "cancel" — user dismissed without choosing.
  action: "accept" | "decline" | "cancel";
  // Present only when action is "accept" and mode was "form".
  content?: { [key: string]: string | number | boolean | string[] };
}

// Counterpart of ListRootsRequest.
// Full definition in §21 Deprecated Client-Provided Capabilities.
interface ListRootsResult {
  roots: { uri: string; name?: string; _meta?: { [key: string]: unknown } }[];
}

// Counterpart of CreateMessageRequest.
// Full definition in §21 Deprecated Client-Provided Capabilities.
interface CreateMessageResult {
  // role, content, model, stopReason, etc. — see §21 Deprecated
  // Client-Provided Capabilities.
  [key: string]: unknown;
}
```

Normative rules for the retry:

- The client **MUST** retry by re-sending the SAME request method as the original request, with the SAME original arguments, plus `inputResponses` and `requestState`. The retry is a new JSON-RPC request with a new request `id` (§3 Base Message Format); it is not a JSON-RPC response.
- The keys in `inputResponses` **MUST** be drawn from the keys present in the `inputRequests` of the `InputRequiredResult` being answered. The client **MUST** answer each input request with its corresponding key; a response under a key absent from `inputRequests` is invalid.
- The `InputResponse` value placed under each key **MUST** be the result counterpart of the `InputRequest` kind the server sent under that key — `ElicitResult` for `"elicitation/create"`, `ListRootsResult` for `"roots/list"`, `CreateMessageResult` for `"sampling/createMessage"`. A client **MUST NOT** answer with a mismatched kind.
- When the client received a `requestState` in the `InputRequiredResult`, it **MUST** include that `requestState`, echoed verbatim (§11.3), in the retry params.
- A retry MAY itself receive another `InputRequiredResult` (with a new `inputRequests` and/or a new `requestState`); the client repeats this process (§11.5).

The `inputResponses` and `requestState` of an `InputRequiredResult` scope solely to the client's retry of the originating request. A client **MUST NOT** attach the `inputResponses` or `requestState` from one `InputRequiredResult` to any other request it issues — including requests it sends concurrently, such as a list operation or an unrelated `tools/call` — and a server **MUST NOT** rely on a client doing so [SEP-2322].

### §11.5 The Exchange Algorithm

The complete MRTR exchange proceeds as follows. Steps are normative.

(a) **Original request.** The client sends the original request (for example `tools/call`, `prompts/get`, or `resources/read`) with its normal arguments and required per-request metadata (§4 Request Metadata and the Stateless Model).

(b) **Server response.** The server returns exactly one of:
- a normal result with `resultType` set to `"complete"` (the request finished; the result holds the final content); or
- an `InputRequiredResult` with `resultType` set to `"input_required"` (§11.2); or
- a JSON-RPC error (§22 Error Handling and Error Codes).

(c) **Fulfillment.** On receiving `"input_required"`, the client, for each entry in `inputRequests`:
1. Verifies the input-request kind against its own declared capabilities (§6 Capabilities and Extensions). The client only fulfills kinds it supports.
2. Fulfills the request locally — for `"elicitation/create"`, by interacting with the user per §20 Elicitation; for `"roots/list"`, by enumerating roots per §21 Deprecated Client-Provided Capabilities; for `"sampling/createMessage"`, by producing a model completion per §21 Deprecated Client-Provided Capabilities — producing the corresponding `InputResponse`.

The client then re-sends the ORIGINAL request method with the original arguments plus an `inputResponses` map (keyed identically to `inputRequests`) and the echoed `requestState` (§11.4).

(d) **Resume.** The server validates `requestState` (§11.3), consumes `inputResponses`, resumes its work, and returns either a `"complete"` result, another `"input_required"` result, or an error (§22 Error Handling and Error Codes).

(e) **Repeat.** Steps (c) and (d) repeat until the server returns a `"complete"` result or an error. There is no protocol-imposed limit on the number of rounds; implementations SHOULD guard against unbounded loops.

**Result-type discrimination.** The `resultType` discriminator (§3 Base Message Format) governs how the client interprets every result in this exchange. A client **MUST** branch on `resultType`: `"complete"` means the final result; `"input_required"` means an `InputRequiredResult` to fulfill. A client **MUST** treat any `resultType` value it does not recognize as an error (§22 Error Handling and Error Codes) and **MUST NOT** attempt to interpret the result body. For interoperability with a peer that omits `resultType` entirely, a client **MUST** treat an absent `resultType` field as if it were `"complete"` (§3 Base Message Format).

**Capability gating.** A server **MUST NOT** emit an `InputRequest` of a kind the client has not declared support for in its capabilities (§6 Capabilities and Extensions). If the server cannot complete the request without an input-request kind the client does not support, the server **SHOULD NOT** emit that kind anyway; instead it **SHOULD** return the missing-required-client-capability error, JSON-RPC error code `-32003`, naming the required capabilities in the error's `data.requiredCapabilities` field (§5 Protocol Revision, Version Negotiation, and Discovery). Over HTTP the response status code for this error **MUST** be `400 Bad Request` (§9 The Streamable HTTP Transport). A client receiving an input-request kind it did not declare **MUST** treat the result as an error (§22 Error Handling and Error Codes).

**Load-shedding (retry-later).** A server MAY return an `InputRequiredResult` that withholds the input requests — that is, with `inputRequests` absent or empty — and carries ONLY a `requestState`. This asks the client to retry later without supplying any new information (for example, while the server completes asynchronous work, such as an out-of-band interaction the server is awaiting). The client distinguishes this case as follows:
- The result has `resultType` `"input_required"`, and
- `inputRequests` is absent or empty, and
- `requestState` is present.

On recognizing a load-shedding result, the client MAY retry the original request immediately, echoing `requestState` verbatim (§11.3) and supplying an empty or omitted `inputResponses` [SEP-2322]. A client that retries repeatedly without making progress SHOULD apply a reasonable backoff and SHOULD provide the user a way to cancel rather than retry. A client **MUST NOT** treat a load-shedding result (which is well-formed: it carries `requestState`) as an error; the requirement that at least one of `inputRequests` or `requestState` be present (§11.2) is what makes this result valid.

**Incomplete or unrecognized input responses.** If the retry's `inputResponses` does not supply all of the information the server still requires to complete the request, the server SHOULD return a new `InputRequiredResult` re-requesting the missing information rather than failing the request; this lets the client always recover by fulfilling the re-issued input requests [SEP-2322]. The server SHOULD ignore any entry in `inputResponses` whose key it does not recognize or does not currently need, treating such entries as optional [SEP-2322]. A response that is malformed at the protocol level (for example invalid JSON, a value that does not match the declared `InputResponse` shape, or an internal failure) is a protocol error and the server returns a JSON-RPC error (§22 Error Handling and Error Codes) rather than an InputRequiredResult.

### §11.6 Methods That Support Multi-Round-Trip Requests

The MRTR pattern applies to client-initiated request methods whose processing may require additional client input. A server MAY return an `"input_required"` result from:

- `tools/call` — tool invocation (§16 Tools).
- `prompts/get` — prompt retrieval (§18 Prompts).
- `resources/read` — resource reads (§17 Resources).

For each of these, the client retries by re-sending the same method with the same original arguments plus `inputResponses` and `requestState` (§11.4). The corresponding sections specify the method-specific `"complete"` result shapes; the `resultType` discriminator (§3 Base Message Format) selects between a `"complete"` result of that shape and an `InputRequiredResult` as defined here.

A client **MUST** be prepared to receive an `"input_required"` result from any of these methods and **MUST** treat an unrecognized `resultType` on any response as an error (§22 Error Handling and Error Codes).

### §11.7 Examples

**Input-required result with one input request and a requestState.** A server processing `tools/call` needs the user's GitHub username before it can finish. It responds with an `InputRequiredResult` carrying a single `elicitation/create` input request under the key `"github-username"`, plus an opaque `requestState`:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "resultType": "input_required",
    "inputRequests": {
      "github-username": {
        "method": "elicitation/create",
        "params": {
          "mode": "form",
          "message": "Please provide your GitHub username",
          "requestedSchema": {
            "type": "object",
            "properties": {
              "name": { "type": "string" }
            },
            "required": ["name"]
          }
        }
      }
    },
    "requestState": "eyJzdGVwIjoiYXdhaXQtdXNlcm5hbWUiLCJzaWciOiI..."
  }
}
```

**Client retry carrying inputResponses and the echoed requestState.** The client elicits the value from the user (§20 Elicitation), then re-sends the ORIGINAL `tools/call` request with its original arguments, adding `inputResponses` (keyed identically — `"github-username"`) and the verbatim `requestState`. The retry is a new request with a new `id`:

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "_meta": {
      "io.modelcontextprotocol/protocolVersion": "2026-07-28"
    },
    "name": "create-repo",
    "arguments": {
      "visibility": "private"
    },
    "inputResponses": {
      "github-username": {
        "action": "accept",
        "content": {
          "name": "octocat"
        }
      }
    },
    "requestState": "eyJzdGVwIjoiYXdhaXQtdXNlcm5hbWUiLCJzaWciOiI..."
  }
}
```

The server validates `requestState`, consumes the response, resumes processing, and returns either a `"complete"` result or a further `"input_required"` result (§11.5). For example, a final completion:

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "resultType": "complete",
    "content": [
      { "type": "text", "text": "Created repository octocat/new-repo." }
    ]
  }
}
```

**Load-shedding (retry-later).** A server awaiting an out-of-band interaction returns an `InputRequiredResult` with no input requests and only a `requestState`, asking the client to retry later:

```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "resultType": "input_required",
    "requestState": "eyJzdGVwIjoiYXdhaXQtY2FsbGJhY2siLCJzaWciOiI..."
  }
}
```

The client recognizes the absence of `inputRequests` together with the presence of `requestState`, waits, and retries the original request echoing `requestState` verbatim with no `inputResponses` (§11.5).

## §12 Pagination

This section defines cursor-based pagination for list operations in the Model Context Protocol [MCP]. Pagination allows a server to return a large result set in smaller successive pages rather than all at once, which is particularly important when results are served over a network but is also useful for local integrations to avoid performance problems with large data sets [MCP].

### §12.1 Cursor Type

A cursor is an opaque string token that represents a position within a result set.

```ts
type Cursor = string;
```

A `Cursor` is a JSON `string`. It is OPAQUE: it carries no externally meaningful structure, and its internal format is determined solely by the issuing server. The empty string `""` is a valid cursor value and MUST be treated as a present cursor, not as the absence of one [MCP].

### §12.2 Request and Result Shapes

A paginated request carries an OPTIONAL `cursor` field in its parameters. A paginated result carries an OPTIONAL `nextCursor` field.

```ts
interface PaginatedRequestParams {
  /**
   * Present on every request. Carries request metadata
   * (see §4 Request Metadata and the Stateless Model).
   */
  _meta?: { [key: string]: unknown };

  /**
   * An opaque token representing the current pagination position.
   * If provided, the server returns results starting after this cursor.
   * Omitted on the request for the first page.
   */
  cursor?: Cursor;
}

interface PaginatedResult {
  /**
   * Reserved metadata (see §14 Common Data Types).
   */
  _meta?: { [key: string]: unknown };

  /**
   * An opaque token representing the pagination position after the
   * last returned result. If present, more results MAY be available.
   * If absent, the current page is the final page.
   */
  nextCursor?: Cursor;
}
```

Field details for `PaginatedRequestParams`:

- `cursor` — OPTIONAL. JSON `string` (a `Cursor`, see §12.1). When present, the server MUST return results positioned after this cursor. When absent, the server MUST return the first page. The field name is exactly `cursor`, case-sensitive.

Field details for `PaginatedResult`:

- `nextCursor` — OPTIONAL. JSON `string` (a `Cursor`, see §12.1). When present, it indicates that additional results MAY be available and provides the token the client uses to request the next page. When absent, the client MUST treat the current page as the last page. The field name is exactly `nextCursor`, case-sensitive.

A paginated result also carries the result fields specific to the list operation (for example, the array of tools, resources, resource templates, or prompts; see §12.5). The `_meta` field on both shapes follows the rules in §4 Request Metadata and the Stateless Model and §14 Common Data Types.

### §12.3 Pagination Semantics

The pagination exchange proceeds as follows [MCP][SEP-2575]:

1. To request the first page, the client sends a paginated list request and omits the `cursor` field (equivalently, the client MAY omit the `params` object entirely if no other parameters are required).
2. The server returns a page of results. Page size is server-determined (see §12.4). If more results exist beyond the returned page, the server includes a `nextCursor` in the result. If the returned page is the last page, the server omits `nextCursor`.
3. To request the next page, the client sends another request of the same method with `cursor` set to the exact `nextCursor` value received in the previous result.
4. The client repeats steps 2–3 until it receives a result that does not contain `nextCursor`. The absence of `nextCursor` marks the end of the result set.

Conformance requirements:

- A client MUST treat a result with no `nextCursor` field as the end of the result set.
- A client MUST NOT treat a present `nextCursor` whose value is the empty string `""` as the end of results; the empty string is a valid cursor and MUST be sent back as `cursor` to continue paginating [MCP].
- A client SHOULD support both paginated and non-paginated flows: a server MAY return the entire result set in a single page (no `nextCursor`), and a client MUST handle that case [MCP].
- A server SHOULD provide stable cursors so that a cursor it issued earlier remains usable to retrieve the page it designates [MCP].
- A server MUST determine, for any given `cursor` value it issued, the position after which results are returned; clients rely only on echoing the server's `nextCursor` back as `cursor`.

Cursor opacity rules (these are normative and central to interoperability) [MCP]:

- Clients MUST treat cursors as opaque tokens. Clients MUST NOT parse, decode, construct, modify, or infer any meaning from a cursor's contents.
- The only determination a client MAY make about a cursor value is whether a non-null value was provided. No other interpretation (such as ordering, page number, count, or completeness) is permitted.
- Clients MUST NOT persist meaning into a cursor and MUST treat a cursor as valid only with the server that issued it. A cursor issued by one server MUST NOT be sent to a different server, and clients MUST NOT assume a cursor remains meaningful across server identities or sessions.

### §12.4 Page Size and Invalid Cursors

Page size is determined entirely by the server. The protocol defines no page-size parameter on requests, and clients MUST NOT assume a fixed page size or that all pages contain the same number of items [MCP]. A server MAY return pages of differing sizes, and MAY return an empty page that nonetheless carries a `nextCursor` (indicating more results MAY follow).

A cursor is invalid if it was not issued by the server, is not recognized by the server, or is otherwise malformed from the server's perspective. When a client supplies an invalid cursor, the server SHOULD respond with a JSON-RPC error whose `code` is `-32602` (Invalid params) [MCP][JSONRPC2]. Servers SHOULD handle invalid cursors gracefully rather than failing catastrophically. The structure, transport, and full set of error codes for such error responses are specified in §22 Error Handling and Error Codes.

### §12.5 Paginated Methods

The following list methods are paginated. Each accepts `PaginatedRequestParams` (with the OPTIONAL `cursor`) and returns a result extending `PaginatedResult` (with the OPTIONAL `nextCursor`):

- `tools/list` — see §16 Tools.
- `resources/list` and `resources/templates/list` — see §17 Resources.
- `prompts/list` — see §18 Prompts.

Each of these methods follows the identical semantics defined in §12.3. The list-specific payload (the tools, resources, resource templates, or prompts array) is defined in the respective section above; the `cursor` and `nextCursor` fields behave uniformly across all of them.

Pagination composes with response caching (see §13 Response Caching). Each page is an independent response identified by its request (including its `cursor` value), and therefore each page is cached independently of every other page. A cached page for one `cursor` value MUST NOT be reused as the response for a request bearing a different `cursor` value (including the first-page request, which omits `cursor`). Any caching hint accompanying a page applies only to that page.

### §12.6 Examples

First-page request. The client omits `cursor` to request the first page of tools:

```json
{
  "jsonrpc": "2.0",
  "id": "123",
  "method": "tools/list",
  "params": {}
}
```

Result with `nextCursor`, indicating more pages are available:

```json
{
  "jsonrpc": "2.0",
  "id": "123",
  "result": {
    "tools": [
      {
        "name": "get_weather",
        "title": "Get Weather",
        "inputSchema": {
          "type": "object",
          "properties": {
            "location": { "type": "string" }
          },
          "required": ["location"]
        }
      }
    ],
    "nextCursor": "eyJwYWdlIjogMn0="
  }
}
```

Follow-up request carrying the received cursor. The client sets `cursor` to the exact `nextCursor` value from the previous result:

```json
{
  "jsonrpc": "2.0",
  "id": "124",
  "method": "tools/list",
  "params": {
    "cursor": "eyJwYWdlIjogMn0="
  }
}
```

Final-page result. The server omits `nextCursor`, signaling the end of the result set:

```json
{
  "jsonrpc": "2.0",
  "id": "124",
  "result": {
    "tools": [
      {
        "name": "get_forecast",
        "title": "Get Forecast",
        "inputSchema": {
          "type": "object",
          "properties": {
            "location": { "type": "string" },
            "days": { "type": "number" }
          },
          "required": ["location"]
        }
      }
    ]
  }
}
```

Error response for an invalid cursor (see §22 Error Handling and Error Codes):

```json
{
  "jsonrpc": "2.0",
  "id": "125",
  "error": {
    "code": -32602,
    "message": "Invalid params: unrecognized cursor"
  }
}
```

## §13 Response Caching

This section defines the optional response-caching hints carried by certain server result objects: a freshness lifetime (`ttlMs`) and a cache-sharing scope (`cacheScope`). These hints let a client (or a shared intermediary) reuse a result it already received instead of re-issuing a request, while remaining safe to ignore [SEP-2549][MCP].

Caching hints are advisory. They do not alter the meaning of any result, are not access-control mechanisms, and never relieve a server of the obligation to return correct data when a request is made. They complement, and do not replace, the change notifications defined in §10 Server-to-Client Streaming and Subscriptions.

### §13.1 The CacheableResult Structure

A *cacheable result* is a result object that augments the base result shape (§3 Base Message Format, §14 Common Data Types) with exactly two caching fields. These two fields are introduced by the `CacheableResult` shape [SEP-2549]:

```ts
interface CacheableResult {
  // Inherited base result fields (see §3 Base Message Format,
  // §14 Common Data Types) also apply, e.g.:
  //   _meta?: { [key: string]: unknown };
  //   resultType: ResultType;

  /**
   * Freshness hint, in milliseconds. See §13.2.
   * MUST be a non-negative integer.
   */
  ttlMs: number;

  /**
   * Cache-sharing scope. See §13.3.
   */
  cacheScope: "public" | "private";
}
```

Field rules:

- `ttlMs` — REQUIRED on every result that carries the cacheable shape. It is a JSON `number` that MUST be a non-negative integer (`ttlMs >= 0`, no fractional part). It carries the meaning defined in §13.2. A receiver that observes a negative value, a non-integer value, or a missing value on a result that is specified to carry it (see §13.4) MUST NOT treat the result as cacheable and SHOULD treat the result as immediately stale.
- `cacheScope` — REQUIRED on every result that carries the cacheable shape. It is a JSON `string` whose value MUST be exactly one of the case-sensitive enumerated strings `"public"` or `"private"`. It carries the meaning defined in §13.3. A receiver that observes any other value, or a missing value, on a result that is specified to carry it (see §13.4) MUST treat the result as if `cacheScope` were `"private"` (the more conservative scope) and SHOULD treat the absence as a malformed caching hint, declining to share the response.

Both fields are present together on a cacheable result; a server MUST NOT emit one without the other on results specified to carry caching hints (§13.4).

The two fields are independent: `ttlMs` governs *for how long* a stored copy MAY be reused, and `cacheScope` governs *by whom* a stored copy MAY be reused. A given result is reusable only while both constraints are satisfied.

### §13.2 ttlMs: Freshness Hint

`ttlMs` is a hint from the server indicating how long, in milliseconds, a client MAY treat the result as fresh before re-fetching. Its semantics are analogous to an HTTP `Cache-Control: max-age` directive [SEP-2549][MCP].

- `ttlMs` MUST be a non-negative integer (`>= 0`).
- If `ttlMs` is `0`, the result SHOULD be considered immediately stale. A client MAY re-fetch the result every time it is needed and SHOULD NOT serve a stored copy as fresh.
- If `ttlMs` is a positive integer N, the client SHOULD consider the result fresh for N milliseconds measured from the client's local receive time of the response, and after that interval SHOULD consider the result stale.

Staleness computation. A client that elects to honor `ttlMs` records the local clock time `receivedAt` at which it received the response, then computes:

```
expiresAt = receivedAt + ttlMs
isFresh(now) = (ttlMs > 0) AND (now < expiresAt)
```

The computation is based solely on the client's own receive time and the `ttlMs` value; it does not depend on any server clock and requires no clock synchronization between client and server. A client MUST NOT assume the two clocks agree.

`ttlMs` is a hint, not a guarantee:

- The underlying data MAY change before the freshness interval elapses; a positive `ttlMs` does not promise that the result remains accurate for that duration. A client that requires the latest state SHOULD re-fetch or rely on the change notifications in §10 Server-to-Client Streaming and Subscriptions (see §13.5).
- A client MAY ignore `ttlMs` entirely (caching nothing, or caching less aggressively), and MAY re-fetch at any time before the interval elapses.
- A client MUST NOT extend reuse beyond the freshness interval merely because the server set a large value; the value is an upper bound on advisory freshness, not a lower bound on storage.

A server SHOULD choose `ttlMs` to reflect how stable the corresponding data is (for example, a small or zero value for rapidly changing data, a larger value for a static catalog).

### §13.3 cacheScope: Cache-Sharing Scope

`cacheScope` is an enumerated `string` indicating the intended sharing scope of a stored copy of the result, analogous to HTTP `Cache-Control: public` versus `Cache-Control: private` [SEP-2549][MCP]. Its permitted values are exactly:

- `"public"` — The result is not specific to a single end-user or credential. Any client or shared intermediary (for example a shared gateway, proxy, or multi-tenant cache that serves multiple end-users or authorization contexts) MAY store the response and serve the stored copy to any user, subject to the freshness interval in §13.2.
- `"private"` — The result is specific to the requesting authorization context. The response MAY be stored and reused only within the single authorization context that made the request (for example the originating end-user's own client). A shared intermediary that serves multiple end-users or credentials MUST NOT serve a stored copy of a `"private"` result to a different user or authorization context.

`cacheScope` is NOT an access-control mechanism. It MUST NOT be used to authorize, restrict, or grant access to data, and a receiver MUST NOT infer any security guarantee from it. Authorization is governed solely by §23 Authorization. A server MUST still return only data the requester is entitled to receive, regardless of `cacheScope`; marking a result `"public"` does not make otherwise-restricted data shareable, and marking a result `"private"` does not by itself protect sensitive data. When in doubt, a server SHOULD choose `"private"`, and a receiver that cannot reliably distinguish authorization contexts MUST treat every cached result as `"private"`.

### §13.4 Results That Carry Caching Hints

The cacheable shape (§13.1) is carried by the following server results, and only these:

- The result of `tools/list` (§16 Tools).
- The result of `prompts/list` (§18 Prompts).
- The result of `resources/list` (§17 Resources).
- The result of `resources/templates/list` (§17 Resources).
- The result of `resources/read` (§17 Resources).

The four list results above also carry the pagination fields defined in §12 Pagination; the `resources/read` result does not.

Server obligation. On each of these results, a server MUST populate both `ttlMs` and `cacheScope` with valid values as defined in §13.1, §13.2, and §13.3. A server that does not wish to encourage caching MUST still include the fields and SHOULD set `ttlMs` to `0`. A server SHOULD set `cacheScope` to `"private"` when the result depends on the requester's authorization context, and MAY set `"public"` only when the result is identical for all requesters.

Other results, notifications, and request objects do not carry `ttlMs` or `cacheScope`. A receiver MUST ignore these fields if they appear on any message that is not one of the results enumerated above (this follows from the general extensibility rule that unknown fields are ignored; see §3 Base Message Format).

Client freedom. A client is free to ignore the caching hints entirely. A client MAY decline to cache any result, MAY cache for a shorter time than `ttlMs` indicates, and MAY re-fetch at any time. A client that honors the hints MUST respect both the freshness bound (§13.2) and the sharing scope (§13.3).

### §13.5 Interaction with Change Notifications and Pagination

Relationship to change notifications. Caching hints complement, and do not replace, the change notifications of §10 Server-to-Client Streaming and Subscriptions. The notifications relevant to the cacheable results are:

- `notifications/tools/list_changed` — affects a cached `tools/list` result.
- `notifications/prompts/list_changed` — affects a cached `prompts/list` result.
- `notifications/resources/list_changed` — affects a cached `resources/list` result and a cached `resources/templates/list` result.
- `notifications/resources/updated` — affects a cached `resources/read` result for the indicated resource URI.

When a client holds a cached result and receives a relevant notification from the server, the client SHOULD invalidate the corresponding cached result and SHOULD re-fetch before relying on it again, even if the result's freshness interval (§13.2) has not yet elapsed. A change notification therefore takes precedence over a still-fresh `ttlMs`: `ttlMs` bounds how long a client MAY reuse a result in the absence of a notification, while a notification SHOULD shorten that reuse. Conversely, the absence of a notification does not by itself extend freshness beyond `ttlMs`, and a server that does not deliver notifications (for example because no subscription or session is in effect) SHOULD set `ttlMs` accordingly.

Relationship to pagination. The four list results are paginated (§12 Pagination): one logical list MAY be delivered across multiple pages, each page being a separate response that carries its own `ttlMs` and `cacheScope`. The following rules apply:

- Each page is cached independently. A client MAY store and expire each page separately according to that page's own `ttlMs`. Pages of the same logical list MAY carry different `ttlMs` values.
- `cacheScope` MUST be consistent across all pages of one logical list. A server MUST NOT mix `"public"` and `"private"` across the pages produced for a single list traversal. A client that observes inconsistent `cacheScope` values across the pages of one logical list MUST treat the entire list as `"private"` (the more conservative scope).
- A page cursor (`nextCursor`) is an opaque token (§12 Pagination); a client MUST NOT parse or interpret it and MUST NOT derive caching behavior from its value. A cache entry for a page is keyed by the request that produced it (including its cursor), not by any structure inferred from the cursor.

### §13.6 Example

The following is a successful `tools/list` response whose result carries both caching hints together with a pagination cursor. The result indicates that the client MAY treat this page as fresh for 600000 milliseconds (10 minutes) and that the page is not user-specific and so MAY be served from a shared cache. The revision string used throughout examples is `2026-07-28`.

```json
{
  "jsonrpc": "2.0",
  "id": "42",
  "result": {
    "resultType": "complete",
    "tools": [
      {
        "name": "get_weather",
        "title": "Get Weather",
        "description": "Return the current weather for a city.",
        "inputSchema": {
          "type": "object",
          "properties": {
            "city": { "type": "string" }
          },
          "required": ["city"]
        }
      }
    ],
    "nextCursor": "eyJwYWdlIjogMn0=",
    "ttlMs": 600000,
    "cacheScope": "public"
  }
}
```

A `resources/read` result that discourages caching sets `ttlMs` to `0` and scopes the response to the single requester:

```json
{
  "jsonrpc": "2.0",
  "id": "43",
  "result": {
    "resultType": "complete",
    "contents": [
      {
        "uri": "file:///home/user/report.txt",
        "mimeType": "text/plain",
        "text": "Quarterly report contents."
      }
    ],
    "ttlMs": 0,
    "cacheScope": "private"
  }
}
```

## §14 Common Data Types

This section defines data structures shared across multiple protocol features: metadata and identity objects, the content block union used in tool results and prompt messages, embedded resource contents, annotation hints, and the role enumeration. Every feature section (for example §16 Tools, §17 Resources, §18 Prompts) composes the types defined here. Field names, type discriminator strings, and enum values are case-sensitive and MUST be reproduced exactly as given [MCP].

Several structures carry an OPTIONAL `_meta` field of JSON object type for implementation-specific metadata. The meaning, reserved key prefixes, and constraints of `_meta` are specified in §4 Request Metadata and the Stateless Model; this section only notes its presence on each structure.

### §14.1 BaseMetadata: name and title

`BaseMetadata` is the common base for objects that have both a programmatic identifier and a human display name. It is not sent on the wire as a standalone message; it contributes its two fields to other structures (for example `Implementation`, and the resource, prompt, and tool descriptors in §16 Tools, §17 Resources, §18 Prompts).

```ts
interface BaseMetadata {
  name: string;    // REQUIRED
  title?: string;  // OPTIONAL
}
```

- `name` (REQUIRED, string): A programmatic or logical identifier. It is intended for use as a stable key in code and protocol references, not as a polished display string.
- `title` (OPTIONAL, string): A human display name optimized to be read and understood by end users, including those unfamiliar with domain-specific terminology.

Display rule [MCP]:

- A consumer that needs to display a name to a human user MUST prefer `title` when it is present.
- When `title` is absent, the consumer MUST use `name` for display.
- An exception applies to tool descriptors: a tool's `annotations.title` (defined in §16 Tools) takes precedence over `name` for display when present. The precedence order for a tool display name is therefore: `title`, then `annotations.title`, then `name`.

`name` MUST NOT be assumed to be unique across an entire implementation unless a feature section states a uniqueness constraint (for example, tool names are unique within a server's tool list per §16 Tools).

### §14.2 Icon and Icons

`Icon` describes a single icon image that a consumer MAY render in a user interface. `Icons` is the mixin contributing an OPTIONAL `icons` array; it is composed into identity and descriptor objects (for example `Implementation`, and the resource, prompt, and tool descriptors).

```ts
interface Icon {
  src: string;                 // REQUIRED
  mimeType?: string;           // OPTIONAL
  sizes?: string[];            // OPTIONAL
  theme?: "light" | "dark";    // OPTIONAL
}

interface Icons {
  icons?: Icon[];              // OPTIONAL
}
```

`Icon` fields:

- `src` (REQUIRED, string): A URI pointing to the icon resource [RFC3986]. It MUST be either an `http`/`https` URL or a `data:` URI whose payload is Base64-encoded image data [RFC4648]. Consumers SHOULD ensure that URLs serving icons are from the same domain as the peer (client or server) or from a trusted domain. Consumers SHOULD apply additional precautions when consuming SVG images, because SVG content can embed executable script.
- `mimeType` (OPTIONAL, string): A MIME type override used when the source MIME type is missing or generic, for example `"image/png"`, `"image/jpeg"`, or `"image/svg+xml"`.
- `sizes` (OPTIONAL, string[]): The sizes at which the icon is intended to be used. Each entry is either a `WxH` pixel specifier (for example `"48x48"` or `"96x96"`) or the literal string `"any"` for scalable formats such as SVG. When omitted, the consumer SHOULD assume the icon is usable at any size.
- `theme` (OPTIONAL, "light" | "dark"): The background theme the icon is designed for. `"light"` indicates the icon is designed for a light background; `"dark"` indicates a dark background. When omitted, the consumer SHOULD assume the icon is usable with any theme.

MIME type support [MCP]:

- A consumer that supports rendering icons MUST support at least `image/png` and `image/jpeg` (the latter also referred to as `image/jpg`).
- A consumer that supports rendering icons SHOULD additionally support `image/svg+xml` and `image/webp`.

Security considerations [MCP]:

- A consumer MUST reject an icon URI that uses an unsafe scheme — including `javascript:`, `file:`, `ftp:`, `ws:`, or any local-application URI scheme — and MUST accept only an `https:` URL or a `data:` URI for the icon source. A consumer MUST NOT follow a scheme change or a redirect to a host on a different origin while fetching an icon.
- A consumer MUST fetch an icon without credentials: it MUST NOT send cookies, an `Authorization` header, or any client credentials with an icon request. A consumer MUST validate an icon's MIME type and file contents before rendering: it MUST treat any declared MIME type as advisory only, MUST detect the actual content type from the file's magic bytes, and MUST reject the icon on a mismatch or an unknown type. A consumer MUST maintain a strict allowlist of permitted image types and MUST reject types outside it.

`Icons` field:

- `icons` (OPTIONAL, Icon[]): A set of sized icons that a consumer MAY display. When absent, no icons are advertised.

### §14.3 Implementation

`Implementation` is the identity object that a client and a server each provide to describe themselves. It is exchanged during version negotiation and discovery (see §5 Protocol Revision, Version Negotiation, and Discovery). It composes `BaseMetadata` (§14.1) and `Icons` (§14.2).

```ts
interface Implementation {
  name: string;          // REQUIRED (from BaseMetadata)
  title?: string;        // OPTIONAL (from BaseMetadata)
  icons?: Icon[];        // OPTIONAL (from Icons)
  version: string;       // REQUIRED
  description?: string;  // OPTIONAL
  websiteUrl?: string;   // OPTIONAL
}
```

- `name` (REQUIRED, string): The programmatic identifier of the implementation (from `BaseMetadata`, §14.1).
- `title` (OPTIONAL, string): The human display name of the implementation (from `BaseMetadata`, §14.1).
- `icons` (OPTIONAL, Icon[]): Icons representing the implementation (from `Icons`, §14.2).
- `version` (REQUIRED, string): The version of the implementation. The format is implementation-defined; it carries no semantics for the protocol.
- `description` (OPTIONAL, string): A human-readable description of what the implementation does, such as the kinds of resources or tools a server provides, or the intended use case of a client.
- `websiteUrl` (OPTIONAL, string): A URL of the implementation's website [RFC3986].

### §14.4 ContentBlock

`ContentBlock` is the discriminated union used for the payload of tool call results and prompt messages. The discriminator is the `type` field, whose string value selects the member. Every member is a JSON object.

```ts
type ContentBlock =
  | TextContent       // type: "text"
  | ImageContent      // type: "image"
  | AudioContent      // type: "audio"
  | ResourceLink      // type: "resource_link"
  | EmbeddedResource; // type: "resource"
```

A receiver MUST dispatch on the exact, case-sensitive value of `type`. A receiver that encounters an unrecognized `type` value SHOULD treat the block as unsupported content rather than failing the entire message, unless a feature section requires otherwise.

The role enumeration that accompanies content in prompt messages is defined in §14.7. Each member is fully specified below.

#### §14.4.1 TextContent

Text provided to or from a language model.

```ts
interface TextContent {
  type: "text";              // REQUIRED, literal
  text: string;              // REQUIRED
  annotations?: Annotations; // OPTIONAL
  _meta?: { [key: string]: unknown }; // OPTIONAL; see §4
}
```

- `type` (REQUIRED, string): MUST be the literal `"text"`.
- `text` (REQUIRED, string): The text content.
- `annotations` (OPTIONAL): Hints for the consumer; see §14.6.
- `_meta` (OPTIONAL, object): Implementation-specific metadata; see §4 Request Metadata and the Stateless Model.

#### §14.4.2 ImageContent

An image provided to or from a language model.

```ts
interface ImageContent {
  type: "image";             // REQUIRED, literal
  data: string;              // REQUIRED, Base64
  mimeType: string;          // REQUIRED
  annotations?: Annotations; // OPTIONAL
  _meta?: { [key: string]: unknown }; // OPTIONAL; see §4
}
```

- `type` (REQUIRED, string): MUST be the literal `"image"`.
- `data` (REQUIRED, string): The image bytes encoded as a Base64 string [RFC4648]. The string MUST contain only valid Base64 characters and MUST decode to the raw image bytes.
- `mimeType` (REQUIRED, string): The MIME type of the image, for example `"image/png"` or `"image/jpeg"`. Different model providers MAY support different image types.
- `annotations` (OPTIONAL): Hints for the consumer; see §14.6.
- `_meta` (OPTIONAL, object): Implementation-specific metadata; see §4 Request Metadata and the Stateless Model.

#### §14.4.3 AudioContent

Audio provided to or from a language model.

```ts
interface AudioContent {
  type: "audio";             // REQUIRED, literal
  data: string;              // REQUIRED, Base64
  mimeType: string;          // REQUIRED
  annotations?: Annotations; // OPTIONAL
  _meta?: { [key: string]: unknown }; // OPTIONAL; see §4
}
```

- `type` (REQUIRED, string): MUST be the literal `"audio"`.
- `data` (REQUIRED, string): The audio bytes encoded as a Base64 string [RFC4648]. The string MUST contain only valid Base64 characters and MUST decode to the raw audio bytes.
- `mimeType` (REQUIRED, string): The MIME type of the audio, for example `"audio/wav"`. Different model providers MAY support different audio types.
- `annotations` (OPTIONAL): Hints for the consumer; see §14.6.
- `_meta` (OPTIONAL, object): Implementation-specific metadata; see §4 Request Metadata and the Stateless Model.

#### §14.4.4 ResourceLink

A reference to a resource by URI, rather than the resource's contents. A `ResourceLink` carries the same descriptive fields as a resource descriptor (see §17 Resources) plus the `type` discriminator. A resource link returned by a tool is not guaranteed to appear in the results of a `resources/list` request (see §17 Resources).

```ts
interface ResourceLink {
  type: "resource_link";     // REQUIRED, literal
  uri: string;               // REQUIRED
  name: string;              // REQUIRED (from BaseMetadata)
  title?: string;            // OPTIONAL (from BaseMetadata)
  icons?: Icon[];            // OPTIONAL (from Icons)
  description?: string;      // OPTIONAL
  mimeType?: string;         // OPTIONAL
  annotations?: Annotations; // OPTIONAL
  size?: number;             // OPTIONAL
  _meta?: { [key: string]: unknown }; // OPTIONAL; see §4
}
```

- `type` (REQUIRED, string): MUST be the literal `"resource_link"`.
- `uri` (REQUIRED, string): The URI of the referenced resource [RFC3986].
- `name` (REQUIRED, string): The programmatic identifier of the resource (from `BaseMetadata`, §14.1).
- `title` (OPTIONAL, string): The human display name of the resource (from `BaseMetadata`, §14.1).
- `icons` (OPTIONAL, Icon[]): Icons representing the resource (from `Icons`, §14.2).
- `description` (OPTIONAL, string): A description of what the resource represents, usable as a hint to a language model.
- `mimeType` (OPTIONAL, string): The MIME type of the resource, if known.
- `annotations` (OPTIONAL): Hints for the consumer; see §14.6.
- `size` (OPTIONAL, number): The size of the raw resource content in bytes, measured before any Base64 encoding or tokenization, if known. Hosts MAY use this to display file sizes and estimate context-window usage.
- `_meta` (OPTIONAL, object): Implementation-specific metadata; see §4 Request Metadata and the Stateless Model.

#### §14.4.5 EmbeddedResource

The contents of a resource embedded directly into a tool result or prompt message. It is the consumer's choice how best to render an embedded resource for a language model and/or a user.

```ts
interface EmbeddedResource {
  type: "resource";          // REQUIRED, literal
  resource: TextResourceContents | BlobResourceContents; // REQUIRED
  annotations?: Annotations; // OPTIONAL
  _meta?: { [key: string]: unknown }; // OPTIONAL; see §4
}
```

- `type` (REQUIRED, string): MUST be the literal `"resource"`.
- `resource` (REQUIRED): The embedded contents, as either a `TextResourceContents` or a `BlobResourceContents` (see §14.5). The variant is determined by which payload field is present (`text` or `blob`).
- `annotations` (OPTIONAL): Hints for the consumer; see §14.6.
- `_meta` (OPTIONAL, object): Implementation-specific metadata; see §4 Request Metadata and the Stateless Model.

### §14.5 ResourceContents and variants

`ResourceContents` is the base for the concrete contents of a resource or sub-resource. It is used both by resource read results (see §17 Resources) and by `EmbeddedResource` (§14.4.5). The base carries identity and type fields; each variant adds exactly one payload field.

```ts
interface ResourceContents {
  uri: string;       // REQUIRED
  mimeType?: string; // OPTIONAL
  _meta?: { [key: string]: unknown }; // OPTIONAL; see §4
}

interface TextResourceContents extends ResourceContents {
  text: string;      // REQUIRED
}

interface BlobResourceContents extends ResourceContents {
  blob: string;      // REQUIRED, Base64
}
```

`ResourceContents` (base) fields:

- `uri` (REQUIRED, string): The URI of the resource these contents belong to [RFC3986].
- `mimeType` (OPTIONAL, string): The MIME type of the resource, if known.
- `_meta` (OPTIONAL, object): Implementation-specific metadata; see §4 Request Metadata and the Stateless Model.

`TextResourceContents` adds:

- `text` (REQUIRED, string): The textual content. This variant MUST be used only when the resource can actually be represented as text rather than as binary data.

`BlobResourceContents` adds:

- `blob` (REQUIRED, string): The binary content encoded as a Base64 string [RFC4648]. The string MUST contain only valid Base64 characters and MUST decode to the raw resource bytes.

A given resource contents value is a text variant if and only if it carries `text`, and a blob variant if and only if it carries `blob`. A receiver MUST select the variant by which of these two fields is present; a single value MUST NOT carry both.

### §14.6 Annotations

`Annotations` carries OPTIONAL hints about a piece of content or a resource. Annotations are attached to content blocks (`TextContent`, `ImageContent`, `AudioContent`, `EmbeddedResource`, `ResourceLink`) and to resource descriptors (see §17 Resources).

```ts
interface Annotations {
  audience?: Role[];     // OPTIONAL
  priority?: number;     // OPTIONAL, 0..1 inclusive
  lastModified?: string; // OPTIONAL, ISO 8601
}
```

- `audience` (OPTIONAL, Role[]): The intended audience for the annotated object, as a list of `Role` values (§14.7). The list MAY contain multiple entries to indicate content useful to multiple audiences, for example `["user", "assistant"]`.
- `priority` (OPTIONAL, number): How important the annotated data is for operating the server. The value MUST be in the inclusive range 0 through 1. A value of 1 means "most important" and indicates the data is effectively required; a value of 0 means "least important" and indicates the data is entirely optional.
- `lastModified` (OPTIONAL, string): The moment the resource was last modified, as an ISO 8601 timestamp string (for example `"2025-01-12T15:00:58Z"`). Typical uses include the last-activity timestamp of an open file or the time a resource was attached.

Trust rule [MCP]: Annotations are hints only. A consumer MUST NOT rely on annotation values for security or correctness decisions, and MUST treat them as untrusted unless they originate from a source the consumer trusts. A consumer MAY use annotations to influence presentation, ordering, or context-inclusion decisions.

### §14.7 Role

`Role` enumerates the sender or recipient of messages and data in a conversation. It is used by `Annotations.audience` (§14.6) and by prompt messages (see §18 Prompts).

```ts
type Role = "user" | "assistant";
```

- `"user"`: The human participant.
- `"assistant"`: The language model participant.

The two listed values are the only permitted values. A receiver MUST treat any other value as invalid.

### §14.8 Extension of the content union by sampling message content

The `ContentBlock` union defined in §14.4 is the content used by tool results and prompt messages. The message content carried by the sampling capability extends the set of content types with two additional members — a tool-use content block (discriminator `type` value `"tool_use"`) and a tool-result content block (discriminator `type` value `"tool_result"`). These two content types are defined, with their full field sets, in §21 Deprecated Client-Provided Capabilities, and are marked deprecated there. They MUST NOT appear in tool call results or prompt messages; the `ContentBlock` union in §14.4 does not include them. Implementations of the structures in this section MUST NOT emit `"tool_use"` or `"tool_result"` blocks where a `ContentBlock` is expected.

### §14.9 Examples

An `Implementation` object (for example, server identity sent during discovery):

```json
{
  "name": "example-files-server",
  "title": "Example Files Server",
  "version": "1.4.2",
  "description": "Provides read access to project files and a search tool.",
  "websiteUrl": "https://example.com/files-server",
  "icons": [
    {
      "src": "https://example.com/icons/files-48.png",
      "mimeType": "image/png",
      "sizes": ["48x48"],
      "theme": "light"
    }
  ]
}
```

A `TextContent` block:

```json
{
  "type": "text",
  "text": "The build completed successfully with 0 warnings."
}
```

An `ImageContent` block (with annotations):

```json
{
  "type": "image",
  "data": "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==",
  "mimeType": "image/png",
  "annotations": {
    "audience": ["user"],
    "priority": 0.3
  }
}
```

An `EmbeddedResource` block carrying `TextResourceContents`:

```json
{
  "type": "resource",
  "resource": {
    "uri": "file:///project/README.md",
    "mimeType": "text/markdown",
    "text": "# Example Project\n\nThis project demonstrates the protocol."
  },
  "annotations": {
    "audience": ["user", "assistant"],
    "priority": 0.8,
    "lastModified": "2026-07-28T09:15:00Z"
  }
}
```

An `Annotations` object:

```json
{
  "audience": ["assistant"],
  "priority": 1,
  "lastModified": "2026-07-28T09:15:00Z"
}
```

## §15 Utilities: Progress, Cancellation, Logging, and Trace Context

This section defines four cross-cutting utility mechanisms that apply to any request or notification regardless of the feature involved: progress reporting, request cancellation, structured log messages, and distributed-trace propagation. All four are realized either as JSON-RPC notifications (see §3 Base Message Format) or as reserved metadata keys carried in the per-request `_meta` envelope (see §4 Request Metadata and the Stateless Model). Each mechanism is OPTIONAL; a receiver that does not implement a given mechanism continues to operate correctly because every mechanism is opt-in. [MCP]

The following common types are referenced throughout this section and are defined in §14 Common Data Types:

```ts
// A uniquely identifying ID for a request in JSON-RPC.
type RequestId = string | number;
```

### §15.1 Progress

#### §15.1.1 The ProgressToken type

A progress token associates progress notifications with the originating request. It is defined as:

```ts
type ProgressToken = string | number;
```

A progress token MUST be either a JSON string or a JSON number. A sender MAY choose the token by any means, but each progress token MUST be unique across all of that sender's currently active requests. Receivers MUST treat the token as an opaque value and MUST NOT attach meaning to its contents. [MCP]

#### §15.1.2 Opting in via request metadata

A party that wishes to *receive* progress updates for a request it issues opts in by including the reserved bare metadata key `progressToken` in that request's `_meta` object (see §4 Request Metadata and the Stateless Model for the `_meta` envelope and reserved-key rules). The relevant portion of the request metadata shape is:

```ts
interface RequestMetaObject {
  // If specified, the caller is requesting out-of-band progress
  // notifications for this request. The value is an opaque token that
  // will be attached to any subsequent progress notifications.
  progressToken?: ProgressToken;
  // ... other reserved request-metadata keys (see §4) ...
}
```

Opting in expresses a desire only. The receiver is NOT obligated to emit any progress notifications. A request that omits `progressToken` MUST NOT receive progress notifications for that request. [MCP]

#### §15.1.3 The notifications/progress notification

The party *processing* a request reports progress by sending the notification whose method name is exactly `notifications/progress`:

```ts
interface ProgressNotification {
  jsonrpc: "2.0";
  method: "notifications/progress";
  params: ProgressNotificationParams;
}

interface ProgressNotificationParams {
  // The progress token given in the originating request's `_meta`,
  // used to associate this notification with that request. REQUIRED.
  progressToken: ProgressToken;
  // The progress thus far. REQUIRED.
  progress: number;
  // Total number of items to process (or total progress required), if
  // known. OPTIONAL.
  total?: number;
  // An optional human-readable message describing current progress.
  message?: string;
  // Optional notification metadata (see §4 / §14).
  _meta?: { [key: string]: unknown };
}
```

Field rules:

- `progressToken` (REQUIRED, `string | number`): MUST equal a token that the peer supplied in the `_meta.progressToken` of an active request the sender is currently processing. A progress notification MUST NOT reference a token that was not supplied by the peer, that does not correspond to an active request, or that is not associated with an in-progress operation. [MCP]
- `progress` (REQUIRED, `number`): The amount of progress made so far. The value MUST increase with each successive notification for the same token, even when `total` is unknown. The value MAY be an integer or a floating-point number.
- `total` (OPTIONAL, `number`): The total amount of progress expected, when known. MAY be an integer or a floating-point number. A receiver MAY omit `total` when the total is unknown.
- `message` (OPTIONAL, `string`): A human-readable description of the current progress. When present, it SHOULD convey relevant progress information suitable for display to a user.

#### §15.1.4 Direction, scope, and delivery

Either party MAY report progress for a request it is currently processing; progress is not restricted to a single direction. The processing party is the sender of `notifications/progress`, and the issuing party is the receiver. [MCP]

Progress notifications are request-scoped. A `notifications/progress` notification MUST be delivered on the response stream of the request whose token it references (see §9 The Streamable HTTP Transport for the stream that carries a request's response, and §10 Server-to-Client Streaming and Subscriptions for stream semantics). A progress notification MUST be sent before the final response for that request.

A receiver of a request carrying a progress token MAY choose to send no progress notifications at all, MAY send them at whatever frequency it deems appropriate, and MAY omit `total`. Both parties SHOULD track active progress tokens, and both parties SHOULD apply rate limiting to progress notifications to avoid flooding the peer. After a request completes, no further `notifications/progress` referencing its token may be sent; a sender MUST stop emitting progress for a token once the associated operation has reached a terminal state. [MCP]

#### §15.1.5 Example: request opting into progress

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "build-index",
    "arguments": { "path": "/data" },
    "_meta": {
      "io.modelcontextprotocol/protocolVersion": "2026-07-28",
      "progressToken": "abc123"
    }
  }
}
```

#### §15.1.6 Example: notifications/progress

```json
{
  "jsonrpc": "2.0",
  "method": "notifications/progress",
  "params": {
    "progressToken": "abc123",
    "progress": 50,
    "total": 100,
    "message": "Reticulating splines..."
  }
}
```

### §15.2 Cancellation

#### §15.2.1 The notifications/cancelled notification

A party that wishes to cancel an in-flight request it issued sends the notification whose method name is exactly `notifications/cancelled`:

```ts
interface CancelledNotification {
  jsonrpc: "2.0";
  method: "notifications/cancelled";
  params: CancelledNotificationParams;
}

interface CancelledNotificationParams {
  // The ID of the request to cancel. MUST correspond to the `id` of a
  // request issued earlier in the same direction.
  requestId?: RequestId;
  // An optional string describing the reason for the cancellation. MAY
  // be logged or presented to the user.
  reason?: string;
  // Optional notification metadata (see §4 / §14).
  _meta?: { [key: string]: unknown };
}
```

Field rules:

- `requestId` (`string | number`): The JSON-RPC `id` of the request being cancelled. It MUST correspond to the `id` of a request that the sender issued earlier in the same direction (a requester cancels a request it sent; it does not cancel requests it received) and that the sender believes is still in-flight. The cancellation MUST reference an in-flight request as understood by the sender. [MCP]
- `reason` (OPTIONAL, `string`): A human-readable explanation of why the request is being cancelled. It MAY be logged or shown to a user.

#### §15.2.2 Semantics

Either party MAY cancel an in-flight request that it issued. When a cancellation notification references the `server/discover` exchange of the protocol-revision handshake, a client MUST NOT cancel that exchange (see §5 Protocol Revision, Version Negotiation, and Discovery). Requests governed by a dedicated cancellation mechanism MUST use that mechanism instead of `notifications/cancelled`; for task-augmented requests the dedicated `tasks/cancel` request MUST be used (see §25 The Tasks Extension). [MCP]

A receiver of `notifications/cancelled` SHOULD:

- stop processing the cancelled request,
- free any resources associated with it, and
- refrain from sending a response for the cancelled request.

A receiver MAY ignore a cancellation notification when the referenced request is unknown, when processing has already completed, or when the request cannot be cancelled. Malformed cancellation notifications SHOULD be ignored. This preserves the fire-and-forget nature of notifications while tolerating asynchronous races. [MCP]

#### §15.2.3 Race conditions

Because of communication latency, a `notifications/cancelled` notification MAY arrive after the referenced request has already finished, and potentially after its response has already been sent. Both parties MUST handle these races gracefully. A response MAY still arrive at the cancelling party after it sent the cancellation; such a response MUST be tolerated, and the cancelling party SHOULD ignore any response to a request it has cancelled. [MCP]

#### §15.2.4 Example: notifications/cancelled

```json
{
  "jsonrpc": "2.0",
  "method": "notifications/cancelled",
  "params": {
    "requestId": "123",
    "reason": "User requested cancellation"
  }
}
```

### §15.3 Logging

> **Deprecated** [SEP-2577]: The logging capability and the per-request logging-message mechanism described in this subsection (§15.3) are Deprecated. Implementations SHOULD NOT rely on this mechanism. Implementations MAY instead write diagnostics to the server's standard-error stream (see §8 The stdio Transport) or use out-of-band observability such as distributed tracing (see §15.4). The mechanism remains specified for interoperability with peers that emit it.

#### §15.3.1 The LoggingLevel enumeration

A logging level is one of the following exact lowercase string values, listed from least to most severe. These map to standard syslog message severities. [MCP]

```ts
type LoggingLevel =
  | "debug"
  | "info"
  | "notice"
  | "warning"
  | "error"
  | "critical"
  | "alert"
  | "emergency";
```

The ordering is significant. A request opts in at a minimum level; the emitting party MUST emit only messages whose level is at or above the requested level in this ordering, where `debug` is the lowest severity and `emergency` is the highest. The conventional meanings are:

| Level       | Meaning                          |
| ----------- | -------------------------------- |
| `debug`     | Detailed debugging information    |
| `info`      | General informational messages    |
| `notice`    | Normal but significant events     |
| `warning`   | Warning conditions                |
| `error`     | Error conditions                  |
| `critical`  | Critical conditions               |
| `alert`     | Action must be taken immediately  |
| `emergency` | System is unusable                |

#### §15.3.2 The notifications/message notification

A server emits a structured log message using the notification whose method name is exactly `notifications/message`:

```ts
interface LoggingMessageNotification {
  jsonrpc: "2.0";
  method: "notifications/message";
  params: LoggingMessageNotificationParams;
}

interface LoggingMessageNotificationParams {
  // The severity of this log message. REQUIRED.
  level: LoggingLevel;
  // An optional name of the logger issuing this message.
  logger?: string;
  // The data to be logged. Any JSON-serializable value is allowed.
  // REQUIRED.
  data: unknown;
  // Optional notification metadata (see §4 / §14).
  _meta?: { [key: string]: unknown };
}
```

Field rules:

- `level` (REQUIRED, `LoggingLevel`): The severity of the message, one of the enumerated strings in §15.3.1.
- `logger` (OPTIONAL, `string`): A name identifying the logger that issued the message.
- `data` (REQUIRED, `unknown`): The payload to be logged. It MAY be any JSON-serializable value, for example a JSON string or a JSON object. Log data MUST NOT contain credentials, secrets, personal identifying information, or internal system details that could aid an attacker. [MCP]

#### §15.3.3 Per-request opt-in and honoring the level

A client opts in to log messages for a single request by including the reserved metadata key `io.modelcontextprotocol/logLevel` in that request's `_meta`, with a value drawn from `LoggingLevel` (see §4 Request Metadata and the Stateless Model). The relevant portion of the request metadata shape is:

```ts
interface RequestMetaObject {
  // The desired minimum log level for this request. OPTIONAL.
  // Deprecated [SEP-2577].
  "io.modelcontextprotocol/logLevel"?: LoggingLevel;
  // ... other reserved request-metadata keys (see §4) ...
}
```

Behavior:

- A server MUST NOT emit any `notifications/message` notification for a request whose `_meta` did not include `io.modelcontextprotocol/logLevel`.
- When the key is present, the server MUST honor the requested level: it MAY emit `notifications/message` notifications only at or above the requested level (per the §15.3.1 ordering), and MUST NOT emit messages below it.
- Logging-message notifications are request-scoped: when emitted, they MUST be delivered on the response stream of the request that set the level (see §9 The Streamable HTTP Transport and §10 Server-to-Client Streaming and Subscriptions), before the final response for that request, and MUST NOT be delivered on any other stream.
- If the value carried at `io.modelcontextprotocol/logLevel` is not one of the recognized `LoggingLevel` strings, the server SHOULD reject that request with the JSON-RPC error code `-32602` (Invalid params); see §22 Error Handling and Error Codes.

Servers SHOULD rate-limit log messages to avoid flooding the client. [MCP]

#### §15.3.4 Example: notifications/message

```json
{
  "jsonrpc": "2.0",
  "method": "notifications/message",
  "params": {
    "level": "error",
    "logger": "database",
    "data": {
      "error": "Connection failed",
      "details": {
        "host": "localhost",
        "port": 5432
      }
    }
  }
}
```

### §15.4 Trace Context

#### §15.4.1 Reserved trace-context metadata keys

Distributed-trace context is propagated through three reserved bare metadata keys carried in the `_meta` object of requests and notifications (see §4 Request Metadata and the Stateless Model for the `_meta` envelope). These keys are unprefixed (bare) names: [W3C-TRACE][W3C-BAGGAGE][OTEL][SEP-414]

```ts
interface TraceContextMeta {
  // W3C Trace Context "traceparent" header value. OPTIONAL.
  traceparent?: string;
  // W3C Trace Context "tracestate" header value. OPTIONAL.
  tracestate?: string;
  // W3C Baggage "baggage" header value. OPTIONAL.
  baggage?: string;
  // ... other metadata keys (see §4) ...
}
```

Field rules:

- `traceparent` (OPTIONAL, `string`): When present, its value follows the standard W3C Trace Context `traceparent` string format, identifying the incoming trace and parent span. [W3C-TRACE][OTEL]
- `tracestate` (OPTIONAL, `string`): When present, its value follows the standard W3C Trace Context `tracestate` string format, carrying vendor-specific trace state. [W3C-TRACE][OTEL]
- `baggage` (OPTIONAL, `string`): When present, its value follows the standard W3C Baggage `baggage` string format, carrying application-defined key/value context. [W3C-BAGGAGE][OTEL]

#### §15.4.2 Propagation rules

These keys MAY appear on the metadata of any request and on the metadata of any notification. They are entirely OPTIONAL. Receivers MUST treat the values as opaque transport for trace propagation: a receiver MUST NOT assume that any of these keys is present, MUST NOT require their presence, and MUST continue to function when they are absent. A receiver that does not participate in tracing MUST ignore these keys without error. [SEP-414]

Intermediaries that relay MCP messages SHOULD propagate `traceparent`, `tracestate`, and `baggage` unchanged from an inbound message onto the corresponding outbound message, so that a single logical operation remains correlatable end to end. [W3C-TRACE][W3C-BAGGAGE][SEP-414]

#### §15.4.3 Example: request metadata carrying traceparent

```json
{
  "jsonrpc": "2.0",
  "id": 7,
  "method": "tools/call",
  "params": {
    "name": "fetch-report",
    "arguments": { "id": "q3" },
    "_meta": {
      "io.modelcontextprotocol/protocolVersion": "2026-07-28",
      "traceparent": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
      "tracestate": "vendorA=t61rcWkgMzE,vendorB=00f067aa0ba902b7",
      "baggage": "userTier=gold,region=us-east-1"
    }
  }
}
```

# Part IV — Server Features

## §16 Tools

Tools are functions a server exposes for invocation by a client, typically under the control of a language model. A tool is identified by a `name`, described by metadata and a JSON Schema for its arguments, and invoked through the `tools/call` request, which returns content and/or a structured value [MCP]. Tools are model-controlled: the protocol does not mandate any particular user-interaction model, but for trust, safety, and security there SHOULD always be a human in the loop able to deny a tool invocation [MCP] (see §28 Security Considerations).

### §16.1 The `tools` server capability

A server that exposes tools MUST declare the `tools` capability in its capabilities object during version negotiation (see §5 Protocol Revision, Version Negotiation, and Discovery and §6 Capabilities and Extensions). The capability is an object with one OPTIONAL sub-flag:

```ts
interface ToolsCapability {
  listChanged?: boolean; // server emits notifications/tools/list_changed
}
```

- `listChanged` (OPTIONAL, boolean): when `true`, the server MAY emit the `notifications/tools/list_changed` notification (see §16.8) when its set of tools changes. When absent or `false`, the server does not emit that notification.

Capability gating (per §6 Capabilities and Extensions):

- A server MUST NOT respond to `tools/list` or `tools/call` unless it has declared the `tools` capability. A client MUST NOT send `tools/list` or `tools/call` to a server that has not declared the `tools` capability.
- A client MUST NOT rely on receiving `notifications/tools/list_changed` unless the server declared `tools.listChanged: true`.

```json
{
  "capabilities": {
    "tools": {
      "listChanged": true
    }
  }
}
```

A server that declares the `tools` capability MUST respond to `tools/list` with the set of tools currently available to the requesting client. This set MAY be empty and MAY change over time, but MUST NOT vary per-connection or as a side effect of other requests on the same connection. The set MAY vary by the authorization presented on the request — for example, returning only the tools the caller's granted scopes permit — because credentials are per-request input, not connection state (see §4 Request Metadata and the Stateless Model) [MCP][SEP-2575].

### §16.2 Listing tools: `tools/list`

To discover available tools, a client sends a `tools/list` request. The request is paginated (see §12 Pagination): its params MAY carry an opaque `cursor` string identifying the pagination position to resume from. The result is both a paginated result and a cacheable result (see §13 Response Caching).

```ts
interface ListToolsRequest {
  method: "tools/list";
  params?: {
    cursor?: string;       // opaque pagination position (§12)
    _meta?: { [key: string]: unknown };
  };
}

// ListToolsResult is a PaginatedResult (§12) AND a CacheableResult (§13).
interface ListToolsResult {
  tools: Tool[];           // REQUIRED
  nextCursor?: string;     // §12: opaque token; if present, more results may exist
  ttlMs: number;           // §13: REQUIRED client-cache TTL hint, in milliseconds, minimum 0
  cacheScope: "public" | "private"; // §13: REQUIRED cache-sharing scope
  resultType: "complete" | "input_required"; // §3: REQUIRED; "complete" for a list result
  _meta?: { [key: string]: unknown }; // OPTIONAL
}
```

Field semantics:

- `tools` (REQUIRED, `Tool[]`): the page of tool definitions. See §16.3.
- `nextCursor` (OPTIONAL, string): an opaque token representing the pagination position after the last returned tool. If present, more results MAY be available and the client MAY issue another `tools/list` request with `cursor` set to this value. If absent, this is the last page. The client MUST treat the value as opaque and MUST NOT parse or construct it (see §12 Pagination).
- `ttlMs` (REQUIRED, number, minimum 0): a hint, in milliseconds, for how long the client MAY cache this list before re-fetching, with semantics analogous to HTTP `Cache-Control: max-age`. A value of `0` means the response SHOULD be considered immediately stale; a positive value means the client SHOULD consider the result fresh for that many milliseconds (see §13 Response Caching) [SEP-2549].
- `cacheScope` (REQUIRED, enum): the intended caching scope. `"public"` means any client or intermediary MAY cache and serve the response to any user; `"private"` means only the requesting user's client MAY cache it, and shared caches MUST NOT serve the cached copy to a different user (see §13 Response Caching) [SEP-2549].
- `resultType` (REQUIRED, string): the result-type discriminator (see §3 Base Message Format). For a tools list the value is `"complete"`.
- `_meta` (OPTIONAL, object): a reserved metadata map (see §14 Common Data Types).

Servers SHOULD return tools in a deterministic order — that is, the same ordering across requests whenever the underlying set of tools has not changed. Deterministic ordering lets clients reliably cache the tool list (see §13 Response Caching) and improves model prompt-cache hit rates when tools are included in model context [MCP].

The change notification associated with this list is described in §16.8 and is delivered over the server-to-client streaming and subscription mechanism of §10 Server-to-Client Streaming and Subscriptions.

Example `tools/list` request:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/list",
  "params": {
    "cursor": "page-2-opaque-token"
  }
}
```

Example `tools/list` result with input and output schemas, caching fields, and a next-page cursor:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "resultType": "complete",
    "tools": [
      {
        "name": "get_weather_data",
        "title": "Weather Data Retriever",
        "description": "Get current weather data for a location",
        "inputSchema": {
          "type": "object",
          "properties": {
            "location": {
              "type": "string",
              "description": "City name or zip code"
            }
          },
          "required": ["location"]
        },
        "outputSchema": {
          "type": "object",
          "properties": {
            "temperature": { "type": "number", "description": "Temperature in celsius" },
            "conditions": { "type": "string", "description": "Weather conditions" },
            "humidity": { "type": "number", "description": "Humidity percentage" }
          },
          "required": ["temperature", "conditions", "humidity"]
        },
        "annotations": {
          "title": "Weather Data Retriever",
          "readOnlyHint": true,
          "openWorldHint": true
        },
        "icons": [
          { "src": "https://example.com/weather-icon.png", "mimeType": "image/png", "sizes": ["48x48"] }
        ]
      }
    ],
    "nextCursor": "next-page-cursor",
    "ttlMs": 300000,
    "cacheScope": "public"
  }
}
```

### §16.3 The `Tool` type

A tool definition has the following shape. It includes the `name`/`title` pair from `BaseMetadata` (see §14 Common Data Types) and the OPTIONAL `icons` array.

```ts
interface Tool {
  // From BaseMetadata (§14):
  name: string;                 // REQUIRED programmatic identifier
  title?: string;               // OPTIONAL human-readable display name

  description?: string;         // OPTIONAL human-readable description

  inputSchema: {                // REQUIRED JSON Schema (2020-12) for the arguments object
    $schema?: string;
    type: "object";
    [key: string]: unknown;
  };

  outputSchema?: {              // OPTIONAL JSON Schema (2020-12) for structuredContent
    $schema?: string;
    [key: string]: unknown;
  };

  annotations?: ToolAnnotations; // OPTIONAL behavior hints (§16.7)

  icons?: Icon[];               // OPTIONAL display icons (§14)

  _meta?: { [key: string]: unknown }; // OPTIONAL
}
```

Field semantics (exact field names, case-sensitive):

- `name` (REQUIRED, string): the unique programmatic identifier used to invoke the tool in a `tools/call` request. Tool names SHOULD be 1 to 128 characters in length inclusive; SHOULD be treated as case-sensitive; SHOULD contain only uppercase and lowercase ASCII letters (`A`–`Z`, `a`–`z`), digits (`0`–`9`), underscore (`_`), hyphen (`-`), and dot (`.`); SHOULD NOT contain spaces, commas, or other special characters; and SHOULD be unique within a single server. Uniqueness is scoped to one server; a client or proxy aggregating tools from multiple servers MAY encounter collisions and SHOULD apply a disambiguation strategy such as prefixing names with a server identifier [MCP].
- `title` (OPTIONAL, string): a human-readable display name. For a tool, the display-name precedence order is `title`, then `annotations.title`, then `name` [MCP].
- `description` (OPTIONAL, string): a human-readable description of the tool's functionality. Clients MAY pass this to the language model as a hint to improve tool selection.
- `inputSchema` (REQUIRED, object): a JSON Schema document describing the tool's arguments object (the `arguments` field of `tools/call`). Because tool arguments are always a JSON object, `type: "object"` is REQUIRED at the schema root. See §16.4 for the full JSON Schema rules. A tool that takes no parameters MUST still provide a valid schema; for example `{ "type": "object", "additionalProperties": false }` to accept only an empty object, or `{ "type": "object" }` to accept any object.
- `outputSchema` (OPTIONAL, object): a JSON Schema document describing the structure of the `structuredContent` field of a `CallToolResult` (see §16.5). See §16.4.
- `annotations` (OPTIONAL, `ToolAnnotations`): untrusted behavior hints. See §16.7.
- `icons` (OPTIONAL, `Icon[]`): icons for display in user interfaces (see §14 Common Data Types).
- `_meta` (OPTIONAL, object): a metadata map reserved for implementation- and extension-specific data (see §14 Common Data Types).

### §16.4 JSON Schema rules for `inputSchema` and `outputSchema`

Both `inputSchema` and `outputSchema` are JSON Schema documents using the JSON Schema 2020-12 dialect. When no explicit `$schema` keyword is present, the document MUST be interpreted as JSON Schema 2020-12 [SEP-2106][JSONSCHEMA]. The following rules are normative:

1. **Dialect and default.** The default dialect is JSON Schema 2020-12. A document MAY declare a different dialect via the `$schema` keyword (for example `"http://json-schema.org/draft-07/schema#"`); when present, that dialect governs interpretation of the document.

2. **Permitted keywords.** The full set of JSON Schema 2020-12 keywords is permitted, including:
   - validation keywords: `type`, `enum`, `const`, `multipleOf`, `maximum`, `exclusiveMaximum`, `minimum`, `exclusiveMinimum`, `maxLength`, `minLength`, `pattern`, `maxItems`, `minItems`, `uniqueItems`, `maxContains`, `minContains`, `maxProperties`, `minProperties`, `required`, `dependentRequired`;
   - applicator/structure keywords: `properties`, `patternProperties`, `additionalProperties`, `propertyNames`, `items`, `prefixItems`, `contains`, `unevaluatedItems`, `unevaluatedProperties`;
   - composition keywords: `allOf`, `anyOf`, `oneOf`, `not`;
   - conditional keywords: `if`, `then`, `else`, `dependentSchemas`;
   - reference keywords: `$ref`, `$defs`, `$anchor`, `$dynamicRef`, `$dynamicAnchor`, `$id`;
   - annotation/metadata keywords: `title`, `description`, `default`, `deprecated`, `readOnly`, `writeOnly`, `examples`, `format`, `contentEncoding`, `contentMediaType`, `contentSchema`.
   For `inputSchema`, any of these keywords MAY appear alongside the REQUIRED root `type: "object"`.

3. **`inputSchema` describes the arguments object.** `inputSchema` describes the `arguments` object supplied to `tools/call` (see §16.5). Its root `type` MUST be `"object"`.

4. **`outputSchema` describes `structuredContent`.** When present, `outputSchema` describes the `structuredContent` value of a `CallToolResult` (see §16.5). It MAY describe any JSON value (its root `type` is not restricted to `"object"`; for example it MAY be `"array"`).

5. **No automatic external dereferencing.** A `$ref` (or `$dynamicRef`) whose target resolves to a location outside the schema document — for example an absolute URI pointing at another resource — MUST NOT be automatically dereferenced or fetched over any network or file system. Only in-document references (those resolving within the same schema document, including `$defs`, `$anchor`, and document-local JSON Pointers) are resolved. An implementation MUST NOT perform network or file-system retrieval to satisfy a reference while validating tool arguments or output [SEP-2106][JSONSCHEMA]. An implementation MAY offer an opt-in mode that fetches non-local `$ref` targets; when offered, that mode MUST be disabled by default, and SHOULD enforce a host allowlist or at minimum reject loopback, link-local, and private-network addresses, apply request timeouts and response size limits, and log every dereferenced URI. A schema that fails to validate because of an unresolved external `$ref` SHOULD be rejected rather than treated as permissive [SEP-2106].

6. **Bounded depth and validation time.** An implementation MUST bound schema nesting depth and validation time so that processing a schema or validating a value cannot exhaust memory, stack, or CPU (resource exhaustion). An implementation MAY impose limits on schema size, nesting depth, reference-resolution count, and per-validation time.

7. **Reject unsafe schemas.** A server MUST reject — or refuse to register — any schema it cannot safely validate, including a schema that exceeds its depth/size/time bounds, a schema that is not a valid JSON Schema object (for example `null`), or a schema requiring external dereferencing it does not permit.

8. **Validation roles.** A server MUST validate `tools/call` arguments against the tool's `inputSchema` before executing the tool, and when `outputSchema` is present MUST produce `structuredContent` conforming to it. A client SHOULD validate received `structuredContent` against the tool's `outputSchema` and, when applying argument validation locally, MUST follow the same in-document-only `$ref` resolution rules stated above [MCP].

9. **Unsupported dialect.** An implementation MUST validate a schema according to its declared or default dialect, and when a schema declares a dialect the implementation does not support, the implementation MUST handle it gracefully by returning an error indicating that the dialect is not supported, rather than silently ignoring the declaration or treating the schema as permissive. An implementation SHOULD document which schema dialects it supports beyond the required JSON Schema 2020-12 [SEP-2106].

The `structuredContent` value itself MAY be any JSON value of any type (object, array, string, number, boolean, or `null`); it is not constrained to objects (see §16.5).

### §16.5 Calling tools: `tools/call`

To invoke a tool, a client sends a `tools/call` request. Its params carry the tool `name`, an OPTIONAL `arguments` object, and the OPTIONAL multi-round-trip retry fields (`inputResponses` and `requestState`) carried by every client-initiated request envelope (see §11 Multi-Round-Trip Requests).

```ts
interface CallToolRequest {
  method: "tools/call";
  params: {
    name: string;                          // REQUIRED tool name
    arguments?: { [key: string]: unknown }; // OPTIONAL arguments object

    // From InputResponseRequestParams (§11):
    inputResponses?: {                     // OPTIONAL responses to a prior input_required result
      [key: string]: unknown;
    };
    requestState?: string;                 // OPTIONAL opaque state echoed back to the server

    _meta?: { [key: string]: unknown };    // OPTIONAL (e.g., progressToken; see §15)
  };
}
```

Request field semantics:

- `name` (REQUIRED, string): the `name` of the tool to invoke. It MUST match the `name` of a tool the server currently exposes to the caller.
- `arguments` (OPTIONAL, object): the arguments object for the call. It MUST validate against the tool's `inputSchema` (see §16.4). When omitted, the server MUST treat it as an empty object `{}`.
- `inputResponses` (OPTIONAL, object): when retrying a call after the server returned an `input_required` result (see §11 Multi-Round-Trip Requests), this map carries the responses to the server's earlier input requests. For each key in that result's `inputRequests`, the same key MUST appear here with the associated response.
- `requestState` (OPTIONAL, string): the opaque state value the server returned in its earlier `input_required` result, echoed back unchanged on retry. The client MUST treat it as an opaque blob and MUST NOT interpret or modify it (see §11 Multi-Round-Trip Requests).
- `_meta` (OPTIONAL, object): a reserved metadata map (see §14 Common Data Types); for example, it MAY carry a `progressToken` (see §15 Utilities: Progress, Cancellation, Logging, and Trace Context).

The result of a successful `tools/call` is either a `CallToolResult` (the call completed) or an `InputRequiredResult` (the server needs further input; see §11 Multi-Round-Trip Requests, signalled by `resultType: "input_required"`).

```ts
interface CallToolResult {
  content: ContentBlock[];      // REQUIRED unstructured result blocks (§14)
  structuredContent?: unknown;  // OPTIONAL structured result; ANY JSON value
  isError?: boolean;            // OPTIONAL; absent ⇒ false (success)
  resultType: "complete" | "input_required"; // §3: REQUIRED; "complete" for a call result
  _meta?: { [key: string]: unknown }; // OPTIONAL
}
```

Result field semantics:

- `content` (REQUIRED, `ContentBlock[]`): the unstructured result of the call, as an array of content blocks. Each block is a `ContentBlock` as defined in §14 Common Data Types (text, image, audio, resource link, or embedded resource). The array MAY be empty and MAY contain blocks of differing types.
- `structuredContent` (OPTIONAL): the structured result. This MAY be ANY JSON value — object, array, string, number, boolean, or `null`. It is explicitly NOT restricted to objects. When the tool declares an `outputSchema`, the server MUST populate `structuredContent` with a value conforming to that schema (see §16.4), and SHOULD also provide a textual `content` fallback (for example a content block of type `text` carrying the JSON serialization of the structured value) for clients that do not consume structured content.
- `isError` (OPTIONAL, boolean): whether the call ended in a tool execution error. When absent, it is assumed `false` (success). See §16.6 for the error model.
- `resultType` (REQUIRED, string): the result-type discriminator (see §3 Base Message Format). For a completed call the value is `"complete"`; an `input_required` result instead carries `"input_required"` and follows §11 Multi-Round-Trip Requests.
- `_meta` (OPTIONAL, object): a reserved metadata map (see §14 Common Data Types).

Example `tools/call` request with arguments:

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "name": "get_weather_data",
    "arguments": {
      "location": "New York"
    }
  }
}
```

Example successful `CallToolResult` with both `content` and `structuredContent` (the tool declares an `outputSchema`, so `structuredContent` conforms to it and a text block carries the serialized JSON fallback):

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "resultType": "complete",
    "content": [
      {
        "type": "text",
        "text": "{\"temperature\": 22.5, \"conditions\": \"Partly cloudy\", \"humidity\": 65}"
      }
    ],
    "structuredContent": {
      "temperature": 22.5,
      "conditions": "Partly cloudy",
      "humidity": 65
    },
    "isError": false
  }
}
```

A tool MAY participate in multi-round-trip flows: a `tools/call` MAY return an `input_required` result (`resultType: "input_required"`) carrying `inputRequests` and/or `requestState`, after which the client gathers the requested input and retries the call with `inputResponses` and `requestState` set as above. The JSON-RPC `id` of the retry MUST differ from the `id` of the initial request (see §11 Multi-Round-Trip Requests).

### §16.6 Error model

Tools use two distinct error-reporting mechanisms, and an implementation MUST distinguish them.

**Tool execution errors** occur when the call reaches the tool but the tool itself fails — for example an upstream API failure, an input value that is well-formed but semantically invalid (a date in the past, a value out of range), or a business-logic failure. These MUST be reported inside a normal successful `CallToolResult` (a JSON-RPC result, not a JSON-RPC error) with `isError` set to `true` and a human- and model-readable explanation in `content`. Reporting tool execution failures this way lets the language model observe the error and self-correct. A client SHOULD provide tool execution errors to the language model to enable self-correction.

**Protocol errors** occur when the request cannot be dispatched to a tool at all — for example an unknown tool name, arguments that fail validation against the `inputSchema`, a malformed `tools/call` request, or a server that does not support tools. These MUST be reported as JSON-RPC errors (see §22 Error Handling and Error Codes), NOT as a `CallToolResult`. Specifically:

- An **unknown tool name** MUST be reported with error code `-32602` (Invalid params).
- **Argument-validation failure** (arguments do not conform to the tool's `inputSchema`) MUST be reported with error code `-32602` (Invalid params).

A client MAY surface protocol errors to the language model, though they are less likely to result in successful recovery.

Example protocol error for an unknown tool (JSON-RPC error, code `-32602`):

```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "error": {
    "code": -32602,
    "message": "Unknown tool: invalid_tool_name"
  }
}
```

Example tool execution error (`CallToolResult` with `isError` true):

```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "result": {
    "resultType": "complete",
    "content": [
      {
        "type": "text",
        "text": "Invalid departure date: must be in the future. Current date is 08/08/2025."
      }
    ],
    "isError": true
  }
}
```

### §16.7 Tool annotations

`ToolAnnotations` carries OPTIONAL, human- and model-oriented hints about a tool's behavior.

```ts
interface ToolAnnotations {
  title?: string;            // OPTIONAL human-readable title
  readOnlyHint?: boolean;    // OPTIONAL; default false
  destructiveHint?: boolean; // OPTIONAL; default true
  idempotentHint?: boolean;  // OPTIONAL; default false
  openWorldHint?: boolean;   // OPTIONAL; default true
}
```

Field semantics:

- `title` (OPTIONAL, string): a human-readable title for the tool, usable for display. In the tool display-name precedence order it ranks after the tool's `title` and before `name` (see §16.3).
- `readOnlyHint` (OPTIONAL, boolean, default `false`): if `true`, the tool does not modify its environment.
- `destructiveHint` (OPTIONAL, boolean, default `true`): if `true`, the tool MAY perform destructive updates to its environment; if `false`, it performs only additive updates. This property is meaningful only when `readOnlyHint` is `false`.
- `idempotentHint` (OPTIONAL, boolean, default `false`): if `true`, calling the tool repeatedly with the same arguments has no additional effect beyond the first call. This property is meaningful only when `readOnlyHint` is `false`.
- `openWorldHint` (OPTIONAL, boolean, default `true`): if `true`, the tool MAY interact with an "open world" of external entities (for example a web-search tool); if `false`, its domain of interaction is closed (for example a memory tool).

All properties in `ToolAnnotations` are HINTS. They are not guaranteed to be a faithful description of tool behavior, including descriptive properties such as `title`. A client MUST treat tool annotations as untrusted and MUST NOT make tool-use or safety decisions based on annotations received from a server it does not trust (see §28 Security Considerations) [MCP].

### §16.8 The `notifications/tools/list_changed` notification

When the set of available tools changes, a server that declared `tools.listChanged: true` (see §16.1) SHOULD send the following notification to clients that are receiving server-to-client messages and have subscribed to tool-list-change updates (see §10 Server-to-Client Streaming and Subscriptions). The notification carries no required payload and MAY be issued without any prior explicit subscription request [MCP][SEP-2575].

```ts
interface ToolListChangedNotification {
  method: "notifications/tools/list_changed";
  params?: {
    _meta?: { [key: string]: unknown };
    [key: string]: unknown;
  };
}
```

On receiving this notification, a client SHOULD invalidate any cached tool list (see §13 Response Caching) and MAY issue a fresh `tools/list` request to obtain the updated set.

Example notification:

```json
{
  "jsonrpc": "2.0",
  "method": "notifications/tools/list_changed"
}
```

### §16.9 Stateful Tools

This subsection is non-normative. The protocol has no per-connection tool session; from the wire's perspective a state handle is an ordinary string in a tool result and an ordinary argument to later tool calls. A server that must relate one tool call to the next — for example a shopping cart, an open browser context, or a database transaction — SHOULD return an explicit handle from a creation tool and accept that handle as an argument on subsequent calls, rather than relying on connection identity (§4.4, §4.5). When designing such handles: for an authenticated server a handle is a name and not a capability, so the server SHOULD validate the caller's authorization against the handle on every call; for an unauthenticated server, where the handle is necessarily a bearer token, the server SHOULD generate it with sufficient entropy (for example a UUIDv4) and give it a bounded lifetime. Handles SHOULD be opaque so they do not invite parsing or guessing, the server's retention policy SHOULD be stated in the creation tool's description so the model can see it, and a call against an expired or unknown handle SHOULD return a tool execution error (§16.6) describing the condition so the model can recover by creating new state [MCP].

## §17 Resources

Resources are server-provided units of context — files, database schemas, documents, or any application-specific data — that a client may discover and read to supply context to a language model. Each resource is identified by a URI [RFC3986]; the URI MAY use any scheme, and it is up to the server how to interpret it. Resources are application-driven: the host decides how and whether to incorporate a resource's contents into a model's context. The protocol does not mandate any user-interaction model for resource selection [MCP].

### §17.1 The `resources` capability

A server that exposes resources MUST declare the `resources` capability in its capabilities object during version negotiation (see §5 Protocol Revision, Version Negotiation, and Discovery and §6 Capabilities and Extensions). The capability is an object with two OPTIONAL boolean sub-flags:

```ts
interface ResourcesServerCapability {
  listChanged?: boolean;  // server emits notifications/resources/list_changed
  subscribe?: boolean;    // server supports per-resource update notifications
}
```

- `listChanged` (OPTIONAL, boolean): when `true`, the server MAY emit a `notifications/resources/list_changed` notification when the set of available resources changes (see §17.7).
- `subscribe` (OPTIONAL, boolean): when `true`, the server supports per-resource update notifications (`notifications/resources/updated`) for resources the client opts into via subscription (see §17.7).

A server MAY advertise either sub-flag independently, both together, or neither. A server that supports neither MAY declare an empty object:

```json
{ "capabilities": { "resources": {} } }
```

Capability gating is governed by §6 Capabilities and Extensions: a server MUST NOT accept `resources/list`, `resources/templates/list`, or `resources/read` requests, and MUST NOT emit `notifications/resources/list_changed` or `notifications/resources/updated`, unless it has declared the `resources` capability. A client MUST NOT issue these requests unless the server declared `resources`. The server MUST NOT emit `notifications/resources/list_changed` unless it declared `listChanged`, and MUST NOT emit `notifications/resources/updated` unless it declared `subscribe` [MCP].

A server that declares the `resources` capability MUST respond to `resources/list` requests with the set of resources currently available to the requesting client. This set MAY be empty and MAY change over time, but MUST NOT vary per-connection or as a side effect of other requests on the connection. The set MAY vary by the authorization presented on the request — for example, returning only the resources the caller's granted scopes permit — since credentials are per-request input, not connection state (see §4 Request Metadata and the Stateless Model) [MCP][SEP-2575].

### §17.2 Listing resources: `resources/list`

To discover available resources, a client sends a `resources/list` request. The request is paginated (see §12 Pagination): its `params` MAY carry a `cursor`.

```ts
interface ListResourcesRequest {       // extends PaginatedRequest (§12)
  method: "resources/list";
  params?: {
    cursor?: string;                   // opaque pagination cursor (§12)
    _meta?: { [key: string]: unknown };
  };
}
```

The result is both a `PaginatedResult` (§12) and a `CacheableResult` (§13), carrying a `resources` array:

```ts
interface ListResourcesResult {        // extends PaginatedResult (§12), CacheableResult (§13)
  resources: Resource[];               // REQUIRED
  nextCursor?: string;                 // §12: present if more pages exist
  resultType: "complete" | "input_required"; // §3: REQUIRED; "complete" for a list result
  ttlMs: number;                       // §13: REQUIRED cache time-to-live in milliseconds, minimum 0
  cacheScope: "public" | "private";    // §13: REQUIRED cache-sharing scope
  _meta?: { [key: string]: unknown };
}
```

- `resources` (REQUIRED, `Resource[]`): array of `Resource` objects (see §17.4). MAY be empty.
- `nextCursor` (OPTIONAL, string): opaque cursor for the next page, per §12 Pagination. When absent, the listing is complete. The client MUST treat the value as opaque and MUST NOT parse or construct it.
- `resultType` (REQUIRED, string): the result-type discriminator (see §3 Base Message Format). For a resources list the value is `"complete"`.
- `ttlMs` (REQUIRED, number, minimum 0) and `cacheScope` (REQUIRED, enum `"public" | "private"`): caching hints governed by §13 Response Caching, with the same semantics as in §16.2 [SEP-2549].
- `_meta` (OPTIONAL, object): a reserved metadata map (see §14 Common Data Types).

The client retrieves the full list by following `nextCursor` until it is absent, exactly as specified in §12 Pagination. A server MUST NOT assume the client has seen any particular page.

### §17.3 Listing resource templates: `resources/templates/list`

To discover parameterized resources, a client sends a `resources/templates/list` request. Like `resources/list`, it is paginated (§12 Pagination) and cacheable (§13 Response Caching).

```ts
interface ListResourceTemplatesRequest {   // extends PaginatedRequest (§12)
  method: "resources/templates/list";
  params?: {
    cursor?: string;
    _meta?: { [key: string]: unknown };
  };
}

interface ListResourceTemplatesResult {    // extends PaginatedResult (§12), CacheableResult (§13)
  resourceTemplates: ResourceTemplate[];   // REQUIRED
  nextCursor?: string;                      // §12
  resultType: "complete" | "input_required"; // §3: REQUIRED; "complete" for a list result
  ttlMs: number;                            // §13: REQUIRED, minimum 0
  cacheScope: "public" | "private";         // §13: REQUIRED
  _meta?: { [key: string]: unknown };
}
```

- `resourceTemplates` (REQUIRED, `ResourceTemplate[]`): array of `ResourceTemplate` objects (see §17.4). MAY be empty.
- `nextCursor`, `resultType`, `ttlMs`, `cacheScope`, `_meta`: behave exactly as in §17.2.

### §17.4 The `Resource` and `ResourceTemplate` types

#### Resource

A `Resource` describes a concrete, directly readable resource. It includes the `BaseMetadata` fields (`name`, `title`) defined in §14 Common Data Types and the icon fields defined in §14 Common Data Types.

```ts
interface Resource {
  uri: string;                  // REQUIRED. The URI of this resource. Format: uri [RFC3986].
  name: string;                 // REQUIRED. Programmatic identifier (BaseMetadata, §14).
  title?: string;               // OPTIONAL. Human-readable display name (BaseMetadata, §14).
  description?: string;         // OPTIONAL. What this resource represents; a hint to the model.
  mimeType?: string;            // OPTIONAL. The MIME type of this resource, if known.
  size?: number;                // OPTIONAL. Size of raw content in bytes (before base64/tokenization), if known.
  annotations?: Annotations;    // OPTIONAL. Hints for the client (§14).
  icons?: Icon[];               // OPTIONAL. Icons for display in user interfaces (§14).
  _meta?: { [key: string]: unknown };  // OPTIONAL. Reserved metadata (§14).
}
```

- `uri` (REQUIRED, string in URI format [RFC3986]): uniquely identifies the resource. The URI MAY use any scheme; the server defines its meaning.
- `name` (REQUIRED, string): the programmatic name of the resource. From `BaseMetadata` (§14 Common Data Types).
- `title` (OPTIONAL, string): a human-readable name for display. From `BaseMetadata` (§14 Common Data Types). When choosing a label to show a user, a client SHOULD prefer `title` and fall back to `name`.
- `description` (OPTIONAL, string): prose describing what the resource represents. Usable as a hint to improve a model's understanding of available resources.
- `mimeType` (OPTIONAL, string): the MIME type of the resource's content, if known.
- `size` (OPTIONAL, number): the size of the raw resource content in bytes, measured before any base64 encoding or tokenization, if known. Hosts MAY use this to display file sizes and estimate context-window usage.
- `annotations` (OPTIONAL, `Annotations`): an `Annotations` object (§14 Common Data Types) carrying hints such as `audience`, `priority`, and `lastModified`.
- `icons` (OPTIONAL, `Icon[]`): an array of `Icon` objects (§14 Common Data Types) for display in user interfaces.
- `_meta` (OPTIONAL, object): a reserved metadata map (§14 Common Data Types).

#### ResourceTemplate

A `ResourceTemplate` describes a family of resources whose URIs are produced by expanding a URI Template [RFC6570]. It also includes `BaseMetadata` (§14 Common Data Types) and icon fields (§14 Common Data Types).

```ts
interface ResourceTemplate {
  uriTemplate: string;          // REQUIRED. A URI Template [RFC6570]. Format: uri-template.
  name: string;                 // REQUIRED (BaseMetadata, §14).
  title?: string;               // OPTIONAL (BaseMetadata, §14).
  description?: string;         // OPTIONAL. What this template is for; a hint to the model.
  mimeType?: string;            // OPTIONAL. MIME type for ALL resources matching this template.
  annotations?: Annotations;    // OPTIONAL (§14).
  icons?: Icon[];               // OPTIONAL (§14).
  _meta?: { [key: string]: unknown };  // OPTIONAL (§14).
}
```

- `uriTemplate` (REQUIRED, string): a string conforming to the URI Template grammar of [RFC6570]. A URI Template contains literal characters interspersed with `{`…`}` expressions naming variables (for example, `file:///{path}` or `db://{table}/{id}`). The client substitutes concrete values for the named variables — by expanding the template according to [RFC6570] — to form a concrete resource `uri` that it then passes to `resources/read` (see §17.5). Variable values MAY be obtained from the user, computed, or suggested via the completion mechanism of §19 Completion.
- `name`, `title`: as for `Resource` (from `BaseMetadata`, §14 Common Data Types).
- `description` (OPTIONAL, string): prose describing the template's purpose; a hint to the model.
- `mimeType` (OPTIONAL, string): a MIME type. It SHOULD be included only if every resource matching the template has the same MIME type.
- `annotations`, `icons`, `_meta`: as for `Resource`.

A `ResourceTemplate` has no `size` field; size is a property of a concrete resource, not of a template.

### §17.5 Reading a resource: `resources/read`

To retrieve a resource's contents, a client sends a `resources/read` request naming the concrete `uri`. The request MAY participate in a multi-round-trip exchange (see §11 Multi-Round-Trip Requests): its params therefore also carry the OPTIONAL retry fields `inputResponses` and `requestState`.

```ts
interface ReadResourceRequestParams {  // extends InputResponseRequestParams (§11)
  uri: string;                         // REQUIRED. URI of the resource to read. Format: uri.
  inputResponses?: { [key: string]: unknown }; // OPTIONAL. Multi-round-trip retry responses (§11).
  requestState?: string;               // OPTIONAL. Opaque server-provided continuation token (§11).
  _meta?: { [key: string]: unknown };
}

interface ReadResourceRequest {
  method: "resources/read";
  params: ReadResourceRequestParams;   // REQUIRED
}
```

- `uri` (REQUIRED, string in URI format): the exact resource to read. It MAY be a concrete resource from `resources/list`, or a URI produced by expanding a `ResourceTemplate` (§17.4).
- `inputResponses` (OPTIONAL, object): present only when the client retries the request to satisfy a server's request for additional input. For each key in the server's earlier `inputRequests`, the same key MUST appear here with the associated response. Structure and semantics are defined in §11 Multi-Round-Trip Requests.
- `requestState` (OPTIONAL, string): the opaque state value the server returned in its earlier `input_required` result, echoed back unchanged on retry. The client MUST treat it as an opaque blob and MUST NOT interpret or modify it. Defined in §11 Multi-Round-Trip Requests.
- `_meta` (OPTIONAL, object): a reserved metadata map (§14 Common Data Types).

The result is a `CacheableResult` (§13) carrying a `contents` array. Each element is a `TextResourceContents` or a `BlobResourceContents` (both defined in §14 Common Data Types):

```ts
interface ReadResourceResult {         // extends CacheableResult (§13)
  contents: (TextResourceContents | BlobResourceContents)[];  // REQUIRED
  resultType: "complete" | "input_required"; // §3: REQUIRED; "complete" for a read result
  ttlMs: number;                       // §13: REQUIRED, minimum 0
  cacheScope: "public" | "private";    // §13: REQUIRED
  _meta?: { [key: string]: unknown };
}
```

Each member of `contents` carries (per §14 Common Data Types):

```ts
interface TextResourceContents {
  uri: string;                  // REQUIRED. URI of this (sub-)resource. Format: uri.
  mimeType?: string;            // OPTIONAL. MIME type, if known.
  text: string;                 // REQUIRED. Text of the item; set only if representable as text.
  _meta?: { [key: string]: unknown };
}

interface BlobResourceContents {
  uri: string;                  // REQUIRED. URI of this (sub-)resource. Format: uri.
  mimeType?: string;            // OPTIONAL. MIME type, if known.
  blob: string;                 // REQUIRED. base64-encoded binary data [RFC4648]. Format: byte.
  _meta?: { [key: string]: unknown };
}
```

Rules:

- `contents` (REQUIRED, array): a server MAY return multiple entries for a single `resources/read` — for example, the contents of several files when a directory resource is read.
- Each entry MUST be either text or binary. A `TextResourceContents` carries its data in the `text` field and MUST set `text` only when the item can actually be represented as text. Binary data MUST use `BlobResourceContents`, carrying its data in the `blob` field as base64-encoded bytes [RFC4648]; a `BlobResourceContents` MUST NOT carry a `text` field. Binary data MUST be properly encoded.
- The `uri` of a returned content entry MAY differ from the requested `uri` (for example, sub-resources under a requested container); each entry identifies the specific (sub-)resource it carries.
- `resultType` (REQUIRED, string): the result-type discriminator (§3 Base Message Format); `"complete"` for a completed read.
- Caching fields (`ttlMs`, `cacheScope`) are governed by §13 Response Caching.

Multi-round-trip: a server MAY respond to `resources/read` with an `input_required` result instead of a `ReadResourceResult`, signalled by `resultType: "input_required"`, indicating that additional input is needed before the resource can be read. This follows §11 Multi-Round-Trip Requests. When the server requests further input, the client retries the same `resources/read` request, including `inputResponses` and — if the server provided one — `requestState` in the params. The discrimination between a normal result and an `input_required` result, and the structure of the `input_required` payload, are defined in §11 Multi-Round-Trip Requests [SEP-2322].

Alternatively, when the scheme of `uri` is `https`, a client MAY fetch the resource directly from the web rather than via `resources/read`; see §17.9 Common URI Schemes [MCP].

A server MUST NOT return an empty `contents` array to signify that a resource does not exist; an empty array is ambiguous (it could mean the resource exists but has no content). Non-existence MUST be reported as the error in §17.6 [MCP].

### §17.6 Resource-not-found error

When a requested `uri` does not correspond to a readable resource, the server MUST return a JSON-RPC error (not a result) with code `-32602` (Invalid params). The error's `data` SHOULD include the offending `uri` field so the client can correlate the failure. Error envelope structure and the full code registry are defined in §22 Error Handling and Error Codes [SEP-2164][MCP]. For interoperability, a client SHOULD also accept `-32002` as a resource-not-found error in addition to `-32602`, because an earlier protocol revision used `-32002` for this condition [MCP].

```ts
interface ResourceNotFoundError {
  code: -32602;                 // Invalid params
  message: string;              // human-readable, e.g. "Resource not found"
  data?: { uri?: string; [key: string]: unknown };  // SHOULD include the requested uri
}
```

A server SHOULD return `-32603` (Internal error, §22 Error Handling and Error Codes) for internal failures unrelated to the validity of the requested `uri`.

### §17.7 Change notifications and subscriptions

Resources support two kinds of server-to-client notifications. Both are delivered through the subscription stream defined in §10 Server-to-Client Streaming and Subscriptions; a client opts into each kind through the filters defined there. There is NO per-resource `subscribe`/`unsubscribe` request method — subscription is governed entirely by §10 Server-to-Client Streaming and Subscriptions.

#### List-changed notification

When the set of available resources changes, a server that declared the `listChanged` sub-flag (§17.1) SHOULD send:

```ts
interface ResourceListChangedNotification {
  method: "notifications/resources/list_changed";
  params?: { _meta?: { [key: string]: unknown }; [key: string]: unknown };  // OPTIONAL
}
```

This notification MAY be issued without any prior subscription action by the client beyond opting into the `resourcesListChanged` filter of §10 Server-to-Client Streaming and Subscriptions. A client opts in by setting `resourcesListChanged: true` in the subscription filter when opening its notification stream per §10 Server-to-Client Streaming and Subscriptions. The server MUST NOT deliver this notification on a stream whose filter did not request `resourcesListChanged`.

#### Per-resource update notification

When a specific resource the client has subscribed to changes and may need to be re-read, a server that declared the `subscribe` sub-flag (§17.1) sends:

```ts
interface ResourceUpdatedNotificationParams {
  uri: string;                  // REQUIRED. URI of the updated resource. Format: uri.
  _meta?: { [key: string]: unknown };
}

interface ResourceUpdatedNotification {
  method: "notifications/resources/updated";
  params: ResourceUpdatedNotificationParams;  // REQUIRED
}
```

- `uri` (REQUIRED, string): the URI of the resource that changed. This MAY be a sub-resource of the URI the client actually subscribed to.

A client opts into per-resource updates by listing the resource URIs it wants to watch in the `resourceSubscriptions` filter of §10 Server-to-Client Streaming and Subscriptions when establishing its notification stream. The server MUST NOT send `notifications/resources/updated` for any resource the client did not opt into via `resourceSubscriptions`. Upon receiving the notification, a client that wants the current contents re-issues `resources/read` for the named `uri` (§17.5). The full mechanics of subscription establishment, acknowledgment of which filters the server honors, subscription-identifier correlation (conveyed in the notification's `_meta`), and cancellation are defined in §10 Server-to-Client Streaming and Subscriptions.

### §17.8 Examples

A `resources/list` result with one resource and caching fields:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "resultType": "complete",
    "resources": [
      {
        "uri": "file:///project/src/main.rs",
        "name": "main.rs",
        "title": "Rust Software Application Main File",
        "description": "Primary application entry point",
        "mimeType": "text/x-rust",
        "size": 920,
        "icons": [
          { "src": "https://example.com/rust-file-icon.png", "mimeType": "image/png", "sizes": ["48x48"] }
        ]
      }
    ],
    "nextCursor": "next-page-cursor",
    "ttlMs": 300000,
    "cacheScope": "public"
  }
}
```

A `resources/templates/list` result with one template:

```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "resultType": "complete",
    "resourceTemplates": [
      {
        "uriTemplate": "file:///{path}",
        "name": "Project Files",
        "title": "Project Files",
        "description": "Access files in the project directory",
        "mimeType": "application/octet-stream"
      }
    ],
    "nextCursor": "next-page-cursor",
    "ttlMs": 300000,
    "cacheScope": "public"
  }
}
```

A `resources/read` request:

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "resources/read",
  "params": {
    "uri": "file:///project/src/main.rs"
  }
}
```

A `resources/read` result with text contents and a cache TTL:

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "resultType": "complete",
    "contents": [
      {
        "uri": "file:///project/src/main.rs",
        "mimeType": "text/x-rust",
        "text": "fn main() {\n    println!(\"Hello world!\");\n}"
      }
    ],
    "ttlMs": 60000,
    "cacheScope": "private"
  }
}
```

A `notifications/resources/updated` notification delivered on the subscription stream of §10 Server-to-Client Streaming and Subscriptions:

```json
{
  "jsonrpc": "2.0",
  "method": "notifications/resources/updated",
  "params": {
    "_meta": { "io.modelcontextprotocol/subscriptionId": "4" },
    "uri": "file:///project/src/main.rs"
  }
}
```

A resource-not-found error:

```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "error": {
    "code": -32602,
    "message": "Resource not found",
    "data": {
      "uri": "file:///nonexistent.txt"
    }
  }
}
```

### §17.9 Common URI Schemes

The protocol defines several standard URI schemes; this list is not exhaustive, and an implementation MAY use additional custom schemes.

- `https://` — a resource available on the web. A server SHOULD use this scheme only when the client is able to fetch and load the resource directly from the web on its own, without reading the resource via the MCP server. For other cases a server SHOULD prefer another scheme, or define a custom one, even if the server itself downloads the contents over the internet.
- `file://` — a resource that behaves like a filesystem; the resource need not map to a physical filesystem. A server MAY identify a `file://` resource with an XDG shared-MIME-info type such as `inode/directory` to represent a non-regular file (for example, a directory) that has no other standard MIME type [MCP].
- `git://` — Git version-control integration.
- Custom schemes — a custom URI scheme MUST conform to [RFC3986] and SHOULD follow the guidance above.

[MCP]

## §18 Prompts

Prompts are server-offered templates that produce structured conversation messages and instructions for interacting with a language model. A server exposes named prompts (optionally accepting arguments) that a client discovers and retrieves [MCP]. Prompts are user-controlled: they are exposed from servers to clients with the intention that a user explicitly selects them for use (for example, surfaced as slash commands). This describes who decides when a prompt is invoked, not who authors its content; prompt content is defined by the server. The protocol does not mandate any specific user-interaction pattern [MCP].

This section defines the `prompts` capability, the `prompts/list` request, the `prompts/get` request, the `notifications/prompts/list_changed` notification, and the associated data types.

### §18.1 The `prompts` capability

A server that offers prompts MUST declare the `prompts` capability in the capabilities object it returns during version negotiation (see §5 Protocol Revision, Version Negotiation, and Discovery and §6 Capabilities and Extensions). Capability gating follows §6 Capabilities and Extensions: a client MUST NOT send `prompts/list` or `prompts/get` to a server that has not declared the `prompts` capability.

The `prompts` capability is an object with one OPTIONAL sub-flag:

```ts
interface PromptsCapability {
  listChanged?: boolean; // server emits notifications/prompts/list_changed
}
```

- `listChanged` (OPTIONAL, boolean): when `true`, the server MAY emit a `notifications/prompts/list_changed` notification (see §18.6) whenever its set of available prompts changes. When absent or `false`, the server MUST NOT be expected to emit that notification. A client MUST NOT rely on receiving list-changed notifications unless the server declared `listChanged: true`.

Declaration example (the surrounding discovery result is defined in §5 Protocol Revision, Version Negotiation, and Discovery):

```json
{
  "capabilities": {
    "prompts": {
      "listChanged": true
    }
  }
}
```

A server that declares the `prompts` capability MUST respond to `prompts/list` with the set of prompts currently available to the requesting client. This set MAY be empty and MAY change over time. The set MUST NOT vary per-connection or as a side effect of other requests on the same connection. The set MAY vary by the authorization presented on the request (for example, returning only the prompts the caller's granted scopes permit), since credentials are per-request input and not connection state (see §4 Request Metadata and the Stateless Model) [MCP][SEP-2575].

### §18.2 Listing prompts: `prompts/list`

To discover available prompts, a client sends a `prompts/list` request. This request is paginated (see §12 Pagination) and its result is cacheable (see §13 Response Caching).

The request method is the exact string `prompts/list`. Its `params` follow the paginated request shape:

```ts
interface ListPromptsRequest {
  method: "prompts/list";
  params?: {
    cursor?: string;                   // opaque pagination position (§12)
    _meta?: { [key: string]: unknown };
  };
}
```

- `cursor` (OPTIONAL, string): an opaque pagination token as defined in §12 Pagination. When provided, the server returns the page of prompts following that cursor. A client MUST treat the cursor value as opaque and MUST NOT construct, parse, or modify it.

The result is a `ListPromptsResult`, which is both a paginated result (§12) and a cacheable result (§13):

```ts
interface ListPromptsResult {
  prompts: Prompt[];                 // REQUIRED; the prompts on this page; MAY be empty
  nextCursor?: string;               // §12: opaque token; if present, more results may exist
  ttlMs: number;                     // §13: REQUIRED client-cache TTL hint, in milliseconds, minimum 0
  cacheScope: "public" | "private";  // §13: REQUIRED cache-sharing scope
  resultType: "complete" | "input_required"; // §3: REQUIRED; "complete" for a list result
  _meta?: { [key: string]: unknown };
}
```

- `prompts` (REQUIRED, `Prompt[]`): the array of prompts on this page. MAY be empty. Each element is a `Prompt` (§18.3).
- `nextCursor` (OPTIONAL, string): opaque pagination token per §12 Pagination. When present, the client MAY issue a follow-up `prompts/list` request with `params.cursor` set to this value to obtain the next page. When absent, there are no further pages at the time of the response. The client MUST treat the value as opaque and MUST NOT parse or construct it.
- `ttlMs` (REQUIRED, number, minimum 0): caching hint per §13 Response Caching. If `0`, the response SHOULD be considered immediately stale and the client MAY re-fetch every time the list is needed. If positive, the client SHOULD consider the result fresh for this many milliseconds after receipt [SEP-2549].
- `cacheScope` (REQUIRED, enum `"public" | "private"`): caching scope per §13 Response Caching. `"public"` permits any client or intermediary (such as a shared gateway or proxy) to cache and serve the response to any user. `"private"` restricts caching to the requesting user's client; shared caches MUST NOT serve a cached copy to a different user [SEP-2549].
- `resultType` (REQUIRED, string): the result-type discriminator (see §3 Base Message Format). For a completed `prompts/list` response, the value is `"complete"`. A server MUST include this field. When a client receives a result lacking `resultType`, it MUST treat the absent field as `"complete"`.
- `_meta` (OPTIONAL, object): a reserved metadata map (§14 Common Data Types). Keys are strings; values are unconstrained JSON.

When the server's prompt set changes, the server SHOULD inform listening clients via the prompts-list-changed notification described in §18.6; the streaming/subscription delivery mechanism for that notification is defined in §10 Server-to-Client Streaming and Subscriptions.

### §18.3 The `Prompt` and `PromptArgument` types

A `Prompt` describes a single prompt or prompt template offered by the server. It carries the common name/title metadata (`BaseMetadata`, §14 Common Data Types) and the optional icon set (§14 Common Data Types):

```ts
interface Prompt {
  name: string;                 // REQUIRED programmatic identifier; fallback display name
  title?: string;               // OPTIONAL human-readable display name
  description?: string;         // OPTIONAL human-readable description
  arguments?: PromptArgument[]; // OPTIONAL arguments accepted for templating
  icons?: Icon[];               // OPTIONAL sized icons for display (§14)
  _meta?: { [key: string]: unknown }; // OPTIONAL
}
```

- `name` (REQUIRED, string): the prompt's programmatic identifier. This is the value a client supplies in `prompts/get` (§18.4). `name` is intended for programmatic or logical use and serves as the display name only as a fallback when `title` is absent.
- `title` (OPTIONAL, string): a human-readable display name optimized for UI and end-user contexts. If not provided, the client SHOULD use `name` for display.
- `description` (OPTIONAL, string): a human-readable description of what the prompt provides.
- `arguments` (OPTIONAL, `PromptArgument[]`): the arguments the prompt accepts for templating. When absent or empty, the prompt accepts no arguments.
- `icons` (OPTIONAL, `Icon[]`): a set of sized icons the client MAY display. Each `Icon` has the shape defined in §14 Common Data Types: a REQUIRED `src` (a URI; an HTTP/HTTPS URL or a `data:` URI with Base64-encoded image data [RFC4648]), an OPTIONAL `mimeType`, an OPTIONAL `sizes` array (each entry in `WxH` form such as `"48x48"`, or `"any"` for scalable formats), and an OPTIONAL `theme` of `"light"` or `"dark"`. Clients that render icons MUST support at least `image/png`, `image/jpeg` (and `image/jpg`), and SHOULD support `image/svg+xml` and `image/webp`. Consumers SHOULD ensure icon URLs come from the same domain as the peer or a trusted domain, and SHOULD take precautions when consuming SVGs, which can contain executable scripts [MCP].
- `_meta` (OPTIONAL, object): a reserved metadata map (§14 Common Data Types).

A `PromptArgument` describes one argument the prompt accepts. It also carries `BaseMetadata` (§14 Common Data Types):

```ts
interface PromptArgument {
  name: string;          // REQUIRED programmatic identifier; key in GetPromptRequest arguments
  title?: string;        // OPTIONAL human-readable display name
  description?: string;  // OPTIONAL human-readable description
  required?: boolean;    // OPTIONAL; whether the argument MUST be provided
}
```

- `name` (REQUIRED, string): the argument's programmatic identifier. This is the key under which the client supplies a value in the `arguments` map of `prompts/get` (§18.4).
- `title` (OPTIONAL, string): a human-readable display name for the argument; if absent, `name` SHOULD be used for display.
- `description` (OPTIONAL, string): a human-readable description of the argument.
- `required` (OPTIONAL, boolean): when `true`, the argument MUST be provided in a `prompts/get` request. When absent or `false`, the argument is optional. If a client omits a required argument, the server SHOULD reject the `prompts/get` request with error code `-32602` (Invalid params) (see §18.4 and §22 Error Handling and Error Codes).

### §18.4 Getting a prompt: `prompts/get`

To retrieve a specific prompt rendered with supplied arguments, a client sends a `prompts/get` request. The request method is the exact string `prompts/get`. A `prompts/get` request MAY participate in a multi-round-trip exchange (see §11 Multi-Round-Trip Requests); its `params` therefore include the multi-round-trip retry fields `inputResponses` and `requestState`.

```ts
interface GetPromptRequest {
  method: "prompts/get";
  params: {
    name: string;                                 // REQUIRED prompt name (matches Prompt.name)
    arguments?: { [key: string]: string };        // OPTIONAL argument values, keyed by PromptArgument.name
    inputResponses?: { [key: string]: unknown };  // OPTIONAL multi-round-trip retry responses (§11)
    requestState?: string;                        // OPTIONAL opaque state echoed back to the server (§11)
    _meta?: { [key: string]: unknown };           // OPTIONAL
  };
}
```

- `name` (REQUIRED, string): the identifier of the prompt to retrieve. It MUST match the `name` of a prompt the server offers. If no such prompt exists, the server SHOULD reject the request with error code `-32602` (Invalid params) (see §22 Error Handling and Error Codes).
- `arguments` (OPTIONAL, object): a map whose keys are argument names (matching `PromptArgument.name`, §18.3) and whose values are the argument values to use for templating. Each value is a JSON string. A client MUST supply a value for every argument the prompt declares with `required: true`. A server SHOULD validate the supplied arguments before processing and SHOULD reject missing required arguments with error code `-32602` (Invalid params).
- `inputResponses` (OPTIONAL, object): a multi-round-trip retry field per §11 Multi-Round-Trip Requests. When the client retries a `prompts/get` after the server returned an `input_required` result, this map carries the responses to the server's prior `inputRequests`. For each key present in the server's `inputRequests`, the same key MUST appear here with the associated response value. Omitted on a first attempt.
- `requestState` (OPTIONAL, string): a multi-round-trip retry field per §11 Multi-Round-Trip Requests. When the server provided a `requestState` blob in its `input_required` result, the client MUST echo that exact value here when retrying. The client MUST treat this as an opaque blob and MUST NOT interpret or modify it. Omitted on a first attempt when the server provided no state.
- `_meta` (OPTIONAL, object): a reserved metadata map (§14 Common Data Types).

The result of a successful, completed `prompts/get` is a `GetPromptResult`:

```ts
interface GetPromptResult {
  description?: string;          // OPTIONAL description for this rendered prompt
  messages: PromptMessage[];     // REQUIRED ordered messages that make up the prompt
  resultType: "complete" | "input_required"; // §3: REQUIRED; "complete" for a completed prompt
  _meta?: { [key: string]: unknown };
}
```

- `description` (OPTIONAL, string): a human-readable description of the rendered prompt.
- `messages` (REQUIRED, `PromptMessage[]`): the ordered list of messages constituting the prompt. MAY contain a single message or several. Each element is a `PromptMessage` (§18.5).
- `resultType` (REQUIRED, string): the result-type discriminator (§3 Base Message Format). For a completed prompt, the value is `"complete"`. A server MUST include this field; a client receiving a result lacking `resultType` MUST treat the absent field as `"complete"`.
- `_meta` (OPTIONAL, object): a reserved metadata map (§14 Common Data Types).

Instead of a `GetPromptResult`, a server MAY respond to `prompts/get` with an `InputRequiredResult` (defined in §11 Multi-Round-Trip Requests), signalled by `resultType: "input_required"`, to indicate that additional input is needed before the prompt can be resolved. In that case the multi-round-trip mechanism of §11 Multi-Round-Trip Requests applies: the client gathers the requested input and retries the original `prompts/get` request, including `inputResponses` and (when the server supplied one) `requestState` in `params`. A client MUST inspect `resultType` to determine whether the response is a `GetPromptResult` (`"complete"`) or an `InputRequiredResult` (`"input_required"`) before parsing it.

Error conditions for `prompts/get` use standard JSON-RPC error codes (see §22 Error Handling and Error Codes): an unknown prompt name and missing required arguments both map to `-32602` (Invalid params) [SEP-2164]; an internal failure maps to `-32603` (Internal error).

### §18.5 The `PromptMessage` type and valid content

A `PromptMessage` is one message within a prompt. It pairs a role with a single content block:

```ts
interface PromptMessage {
  role: "user" | "assistant"; // REQUIRED; the Role enumeration (§14)
  content: ContentBlock;      // REQUIRED; exactly one content block (§14)
}
```

- `role` (REQUIRED, string): one of the two values of the `Role` enumeration defined in §14 Common Data Types: `"user"` or `"assistant"`. It indicates the speaker of the message.
- `content` (REQUIRED, `ContentBlock`): exactly one content block, as defined by the `ContentBlock` union in §14 Common Data Types. `content` is a single object, not an array.

The `ContentBlock` union (§14 Common Data Types) admits the following content kinds, all of which are valid in a prompt message:

- Text content (`type: "text"`): plain text, carried in a `text` string field. This is the most common kind for natural-language interactions.
- Image content (`type: "image"`): visual data carried Base64-encoded in `data` with a `mimeType` such as `image/png` [RFC4648].
- Audio content (`type: "audio"`): audio data carried Base64-encoded in `data` with a `mimeType` such as `audio/wav` [RFC4648].
- Resource link (`type: "resource_link"`): a reference to a server resource by `uri`, without embedding its contents, supplying additional context the client MAY fetch.
- Embedded resource (`type: "resource"`): server-side resource contents embedded directly via a `resource` object that is either text contents or Base64-encoded blob contents.

The full field shapes, required fields, and constraints for each content kind (including the `annotations` field carrying audience/priority/lastModified hints) are defined normatively in §14 Common Data Types and are not restated here. Embedded and linked resource semantics are defined in §17 Resources.

### §18.6 The prompts-list-changed notification

When the set of available prompts changes, a server that declared `listChanged: true` (§18.1) SHOULD send a `notifications/prompts/list_changed` notification to clients that are listening for it. This is a one-way JSON-RPC notification: it has no `id` and no response.

```ts
interface PromptListChangedNotification {
  method: "notifications/prompts/list_changed";
  params?: {
    _meta?: { [key: string]: unknown };
  };
}
```

- `method` (REQUIRED, string): the exact string `notifications/prompts/list_changed`.
- `params` (OPTIONAL, object): when present, it MAY carry only a reserved `_meta` map (§14 Common Data Types). The notification carries no prompt data itself.

A server MAY issue this notification without any prior explicit subscription from the client. The delivery and subscription mechanics — including the listening stream over which this notification is delivered — are defined in §10 Server-to-Client Streaming and Subscriptions. On receiving this notification, a client SHOULD invalidate any cached prompt list and MAY re-issue `prompts/list` (§18.2) to obtain the current set. A server that did not declare `listChanged: true` MUST NOT be expected to emit this notification.

Notification example:

```json
{
  "jsonrpc": "2.0",
  "method": "notifications/prompts/list_changed"
}
```

### §18.7 Argument completion

Prompt arguments are completable. A client MAY request auto-completion suggestions for the value of a prompt argument through the Completion utility defined in §19 Completion. A completion request references the prompt by name and the target argument by name and supplies the partial value entered so far; the server returns candidate completions. The completion request/result wire shapes, the reference type used to identify a prompt argument, and the completion capability gating are defined normatively in §19 Completion and are not restated here.

### §18.8 Examples

A `prompts/list` result declaring a prompt with arguments, icons, and caching fields:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "resultType": "complete",
    "prompts": [
      {
        "name": "code_review",
        "title": "Request Code Review",
        "description": "Asks the LLM to analyze code quality and suggest improvements",
        "arguments": [
          {
            "name": "code",
            "description": "The code to review",
            "required": true
          }
        ],
        "icons": [
          {
            "src": "https://example.com/review-icon.svg",
            "mimeType": "image/svg+xml",
            "sizes": ["any"]
          }
        ]
      }
    ],
    "nextCursor": "next-page-cursor",
    "ttlMs": 600000,
    "cacheScope": "public"
  }
}
```

A `prompts/get` request supplying arguments:

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "prompts/get",
  "params": {
    "name": "code_review",
    "arguments": {
      "code": "def hello():\n    print('world')"
    }
  }
}
```

A successful `GetPromptResult` carrying messages:

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "resultType": "complete",
    "description": "Code review prompt",
    "messages": [
      {
        "role": "user",
        "content": {
          "type": "text",
          "text": "Please review this Python code:\ndef hello():\n    print('world')"
        }
      }
    ]
  }
}
```

A `prompts/get` request retried with multi-round-trip fields (see §11 Multi-Round-Trip Requests), after the server responded with an `input_required` result requesting additional input keyed `"confirm"`:

```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "prompts/get",
  "params": {
    "name": "code_review",
    "arguments": {
      "code": "def hello():\n    print('world')"
    },
    "inputResponses": {
      "confirm": { "action": "accept", "content": { "approved": true } }
    },
    "requestState": "opaque-server-state-blob"
  }
}
```

## §19 Completion

Completion lets a server offer autocompletion suggestions for the individual arguments of a prompt or a resource template. As a user fills in an argument value — for example in a dropdown that filters as the user types — the client asks the server for ranked candidate completions of the current partial value, optionally supplying already-resolved sibling arguments as disambiguating context [MCP]. The single completion method is `completion/complete`, sent from client to server.

The referenced prompt is defined in §18 Prompts; the referenced resource template is defined in §17 Resources. The base request envelope and `_meta` rules are defined in §3 Base Message Format and §4 Request Metadata and the Stateless Model. The `resultType` field on every result is defined in §3 Base Message Format.

### §19.1 The `completions` capability

A server that supports argument autocompletion MUST advertise the `completions` capability in its capabilities object during version negotiation, as governed by §6 Capabilities and Extensions [MCP]. The capability value is a JSON object; its presence (not its contents) is what signals support. The empty object `{}` is the minimum baseline and the RECOMMENDED value.

```ts
interface CompletionsCapability {
  [key: string]: unknown;
}
```

A client MUST NOT send a `completion/complete` request to a server that has not advertised the `completions` capability. A server that has not advertised `completions` and receives a `completion/complete` request SHOULD respond with the JSON-RPC error code `-32601` (Method not found), as defined in §22 Error Handling and Error Codes.

Minimum baseline declaration:

```json
{
  "capabilities": {
    "completions": {}
  }
}
```

### §19.2 `completion/complete` request

The method name is the exact, case-sensitive string `completion/complete`. The request is a JSON-RPC request as defined in §3 Base Message Format.

```ts
interface CompleteRequest {
  method: "completion/complete";
  params: CompleteRequestParams;
}

interface CompleteRequestParams {
  ref: PromptReference | ResourceTemplateReference; // REQUIRED; what is being completed
  argument: {                                        // REQUIRED; the argument being completed
    name: string;                                    // REQUIRED; argument name
    value: string;                                   // REQUIRED; current partial value (match seed)
  };
  context?: {                                         // OPTIONAL; additional completion context
    arguments?: { [key: string]: string };           // OPTIONAL; already-resolved sibling arguments
  };
  _meta?: { [key: string]: unknown };                // OPTIONAL request metadata (§4)
}
```

Field rules:

- `ref` (REQUIRED): a discriminated union identifying what is being completed; the discriminator is the `type` field. It MUST be either a `PromptReference` (`type` equal to `"ref/prompt"`) or a `ResourceTemplateReference` (`type` equal to `"ref/resource"`), as defined in §19.3. A receiver MUST select the variant by the value of `ref.type` and MUST reject any other `type` value.
- `argument` (REQUIRED, object): an object describing the single argument the client is completing.
  - `argument.name` (REQUIRED, string): the name of the argument being completed. For a prompt, this is one of the prompt's declared argument names (see §18 Prompts). For a resource template, this is the name of a variable appearing in the URI template (see §17 Resources).
  - `argument.value` (REQUIRED, string): the current partial value the user has entered for that argument. It is the seed/prefix against which the server matches candidates. It MAY be the empty string `""`, in which case the server SHOULD return suggestions appropriate to an empty input.
- `context` (OPTIONAL, object): carries additional information to refine completion.
  - `context.arguments` (OPTIONAL, object): a map of `string` keys to `string` values. Each key is the name of a sibling argument (of the same prompt or resource template) that has already been resolved, and each value is its resolved value. A client SHOULD populate this map with already-entered argument values so the server can produce context-sensitive suggestions (for example, completing a `framework` argument differently once `language` is known). Keys MUST NOT include the argument named in `argument.name`. Servers MAY ignore the context entirely.
- `_meta` (OPTIONAL, object): request metadata, as defined in §4 Request Metadata and the Stateless Model.

### §19.3 Reference types: `PromptReference` and `ResourceTemplateReference`

`ref` is a discriminated union over the string field `type`. There are exactly two variants:

```ts
// Identifies a prompt by name. Discriminator: type === "ref/prompt".
interface PromptReference {
  type: "ref/prompt";
  name: string;   // REQUIRED; programmatic name of the prompt (see §18 Prompts)
  title?: string; // OPTIONAL; human-readable display name (§14)
}

// Identifies a resource or resource template. Discriminator: type === "ref/resource".
interface ResourceTemplateReference {
  type: "ref/resource";
  uri: string;    // REQUIRED; URI or URI template of the resource (see §17 Resources)
}
```

`PromptReference`:

- `type` (REQUIRED): MUST equal the exact string `"ref/prompt"`.
- `name` (REQUIRED, string): the programmatic name of the prompt being completed, matching a prompt exposed via the prompt-listing facilities of §18 Prompts.
- `title` (OPTIONAL, string): a human-readable display name. It is not load-bearing for completion matching; servers resolve the prompt by `name`.

`ResourceTemplateReference`:

- `type` (REQUIRED): MUST equal the exact string `"ref/resource"`.
- `uri` (REQUIRED, string): the URI or URI template of the resource whose variables are being completed, matching a resource template exposed per §17 Resources. The value MAY be a literal URI or a URI template containing variables (for example `file:///{path}`). When it is a URI template, `argument.name` identifies the template variable being completed.

A receiver MUST treat the union as closed: a `ref` whose `type` is neither `"ref/prompt"` nor `"ref/resource"` is invalid and MUST be rejected with error code `-32602` (Invalid params), as defined in §22 Error Handling and Error Codes.

### §19.4 `CompleteResult`

The successful result of `completion/complete` carries a single `completion` object holding the ranked candidate values.

```ts
interface CompleteResult {
  completion: {
    values: string[];   // REQUIRED; candidate values, ranked; MUST NOT exceed 100 items
    total?: number;     // OPTIONAL; total matches available, MAY exceed values.length
    hasMore?: boolean;  // OPTIONAL; whether more matches exist beyond values
  };
  resultType: "complete" | "input_required"; // §3: REQUIRED; "complete" for this result
  _meta?: { [key: string]: unknown };          // OPTIONAL result metadata (§4)
}
```

Field rules:

- `completion` (REQUIRED, object): the object containing the suggestions.
  - `completion.values` (REQUIRED, `string[]`): the candidate completion values, ordered by descending relevance (see §19.5). The array MUST NOT contain more than 100 items. A server with more than 100 matches MUST cap `values` at 100 items and SHOULD set `hasMore` to `true` (and MAY set `total`). The array MAY be empty when there are no matches.
  - `completion.total` (OPTIONAL, number): the total number of matching options available. This value MAY exceed the number of items actually returned in `values`. If omitted, the total is unknown.
  - `completion.hasMore` (OPTIONAL, boolean): indicates whether additional matches exist beyond those returned in `values`, even when `total` is not provided. If omitted, clients SHOULD treat it as `false`.
- `resultType` (REQUIRED, string): the result-type discriminator, as defined in §3 Base Message Format. A server MUST include it; for a successful completion the value is `"complete"`. A client receiving a result that omits `resultType` MUST treat the absent field as `"complete"`.
- `_meta` (OPTIONAL, object): result metadata, as defined in §4 Request Metadata and the Stateless Model.

### §19.5 Behavior

Completion is best-effort and advisory. Suggestions are not authoritative validation of the argument; a client MAY submit a value that does not appear in any completion result, and a server MUST NOT treat an absent value as forbidden solely because completion did not surface it [MCP].

Server behavior:

- A server SHOULD return candidates in `completion.values` ranked by relevance, most relevant first.
- A server SHOULD perform matching against `argument.value` (for example prefix, substring, or fuzzy matching) as appropriate to the argument.
- A server SHOULD use `context.arguments`, when present, to disambiguate and refine suggestions for the argument named in `argument.name`. A server MAY ignore `context`.
- A server MUST cap `completion.values` at 100 items (§19.4) and SHOULD signal truncation via `hasMore` (and MAY provide `total`).
- A server SHOULD validate all inputs and SHOULD rate-limit completion requests, returning `-32603` (Internal error) for internal failures, as defined in §22 Error Handling and Error Codes.
- A server MUST NOT use completion to disclose sensitive values to which the requester is not entitled; access control over suggested values is REQUIRED, per §28 Security Considerations.

Client behavior:

- A client SHOULD populate `context.arguments` with already-resolved sibling arguments to obtain context-sensitive suggestions across a multi-argument prompt or template.
- A client SHOULD debounce rapid successive completion requests and MAY cache results.
- A client MUST handle empty, partial, or missing-field results gracefully (for example an empty `values` array, or omitted `total`/`hasMore`).

Error handling (codes per §22 Error Handling and Error Codes):

- If the server has not advertised the `completions` capability, it SHOULD respond with `-32601` (Method not found).
- If `ref` identifies an unknown prompt (no prompt with the given `name`) or an unknown resource template (no template matching the given `uri`), or if `argument.name` is not a valid argument of the referenced prompt or template, the server MUST respond with `-32602` (Invalid params). Unknown references to completion targets are reported via Invalid Params rather than as a not-found result [SEP-2164].
- If required parameters are missing or malformed (for example a missing `ref`, a `ref.type` outside the closed union, or a missing `argument.name`/`argument.value`), the server MUST respond with `-32602` (Invalid params).
- For internal failures while computing completions, the server SHOULD respond with `-32603` (Internal error).

### §19.6 Examples

Request — completing a prompt argument with a partial value and disambiguating context. The user is completing the `framework` argument of the `code_review` prompt, having already chosen `language` = `python`:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "completion/complete",
  "params": {
    "ref": {
      "type": "ref/prompt",
      "name": "code_review"
    },
    "argument": {
      "name": "framework",
      "value": "fla"
    },
    "context": {
      "arguments": {
        "language": "python"
      }
    }
  }
}
```

Result — candidate values with `total` and `hasMore` indicating more matches than were returned:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "resultType": "complete",
    "completion": {
      "values": ["python", "pytorch", "pyside"],
      "total": 10,
      "hasMore": true
    }
  }
}
```

Request — completing a resource template variable. The `path` variable of a URI template is being completed:

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "completion/complete",
  "params": {
    "ref": {
      "type": "ref/resource",
      "uri": "file:///{path}"
    },
    "argument": {
      "name": "path",
      "value": "src/comp"
    }
  }
}
```

Result — a single exhaustive match:

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "resultType": "complete",
    "completion": {
      "values": ["src/completion.ts"],
      "total": 1,
      "hasMore": false
    }
  }
}
```

Error — the referenced prompt does not exist:

```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "error": {
    "code": -32602,
    "message": "Unknown prompt: no prompt named 'code_reviw'"
  }
}
```

# Part V — Client Features

## §20 Elicitation

Elicitation lets a server request structured input from the user, gathered and returned through the client. It is one of the three kinds of client-provided input a server may request during the processing of a client request; the other two are defined in §21 Deprecated Client-Provided Capabilities. Elicitation is an active (non-Deprecated) client capability. [MCP]

An elicitation request is NOT delivered as a server-initiated JSON-RPC request. A server requests elicitation by returning an input-required result, and the client supplies the user's input by retrying the originating request. This is the multi-round-trip mechanism defined in §11 Multi-Round-Trip Requests. The method discriminator that identifies an elicitation input request is `elicitation/create`. [SEP-2322][MCP]

### §20.1 Capability declaration and gating

A client that supports elicitation MUST declare the `elicitation` capability. Per the stateless model, capabilities are advertised on every request in the per-request metadata envelope; the elicitation capability is carried under the reserved metadata key `io.modelcontextprotocol/clientCapabilities` (see §4 Request Metadata and the Stateless Model and §6 Capabilities and Extensions for the gating and envelope rules). [SEP-2575][MCP]

The capability value is an object with two OPTIONAL sub-flags, each selecting a supported elicitation mode:

```ts
ClientCapabilities {
  // ... other capability fields defined in §6 ...
  elicitation?: {
    form?: { [key: string]: JSON };  // present if form mode is supported
    url?: { [key: string]: JSON };   // present if url mode is supported
  };
}
```

Each sub-flag, when present, is an object. An empty object (`{}`) indicates support for that mode with no additional settings. [MCP]

Rules:

- A client that declares the `elicitation` capability MUST support at least one mode (`form` or `url`). [MCP]
- For backwards compatibility, an empty `elicitation` capability object (`{}`) is equivalent to declaring support for `form` mode only. That is, `"elicitation": {}` is treated identically to `"elicitation": { "form": {} }`. [MCP]
- Servers MUST NOT send elicitation requests whose `mode` is not supported by the client, as determined by the declared sub-flags (with the empty-object equivalence above). [MCP]
- A server MUST NOT return an `elicitation/create` input-required result to a client that has not declared the `elicitation` capability. The general capability-gating rules of §6 Capabilities and Extensions apply.

Capability declaration example:

```json
{
  "capabilities": {
    "elicitation": {
      "form": {},
      "url": {}
    }
  }
}
```

Form-mode-only declaration (implicit form support via the empty object):

```json
{
  "capabilities": {
    "elicitation": {}
  }
}
```

### §20.2 Delivery via input-required result

A server MAY request information from a user during the processing of a client request by returning an input-required result that contains an `elicitation/create` request, as defined in §11 Multi-Round-Trip Requests. The result-type discriminator that marks a result as input-required is defined in §3 Base Message Format. The client gathers the user's input and supplies it by retrying the originating request, carrying the collected result in the input responses of the retry (the round-trip envelope and correlation are defined in §11). [SEP-2322][MCP]

The embedded request has the following shape:

```ts
ElicitRequest {
  method: "elicitation/create";  // REQUIRED; exact literal, case-sensitive
  params: ElicitRequestParams;   // REQUIRED
}
```

`params` is one of two mode-specific shapes (the union `ElicitRequestParams`):

```ts
ElicitRequestParams = ElicitRequestFormParams | ElicitRequestURLParams;
```

The mode is selected by the `mode` discriminator field within `params`, defined per shape in §20.3.

### §20.3 Elicitation modes and parameter shapes

Two modes are defined. The discriminator is the `mode` field. [MCP]

- `"form"`: In-band structured data collection with an optional schema. The collected data IS exposed to the client.
- `"url"`: Out-of-band interaction via navigation to a URL. Data other than the URL itself is NOT exposed to the client; it is suited to sensitive or secure operations such as authorization or payment flows.

#### Form mode parameters

```ts
ElicitRequestFormParams {
  mode?: "form";        // OPTIONAL; if present MUST be the literal "form"
  message: string;      // REQUIRED; human-readable text describing what is requested
  requestedSchema: {    // REQUIRED; restricted JSON Schema (see §20.4)
    $schema?: string;   // OPTIONAL; JSON Schema dialect identifier
    type: "object";     // REQUIRED; MUST be the literal "object"
    properties: { [key: string]: PrimitiveSchemaDefinition };  // REQUIRED
    required?: string[]; // OPTIONAL; names of properties that MUST be supplied
  };
}
```

Field rules:

- `mode`: OPTIONAL. If present, MUST be the literal string `"form"`. For backwards compatibility, a server MAY omit `mode` for a form-mode request. A client MUST treat a request with no `mode` field as form mode. [MCP]
- `message`: REQUIRED string. The text to present to the user describing what information is being requested. [MCP]
- `requestedSchema`: REQUIRED object describing the fields to collect. Its `type` MUST be `"object"`. Its `properties` is a map whose values are `PrimitiveSchemaDefinition` values (see §20.4); this is a flat, non-nested schema. `required` is an OPTIONAL array of property names that MUST be supplied. `$schema` is an OPTIONAL string naming the JSON Schema dialect. [MCP][JSONSCHEMA]

#### URL mode parameters

```ts
ElicitRequestURLParams {
  mode: "url";            // REQUIRED; MUST be the literal "url"
  message: string;        // REQUIRED; human-readable text explaining why the interaction is needed
  elicitationId: string;  // REQUIRED; opaque correlation identifier
  url: string;            // REQUIRED; the URL the user should navigate to
}
```

Field rules:

- `mode`: REQUIRED. MUST be the literal string `"url"`. [MCP]
- `message`: REQUIRED string. The text to present to the user explaining why the interaction is needed. [MCP]
- `elicitationId`: REQUIRED string. Uniquely identifies the elicitation within the context of the server. The client MUST treat this value as opaque. It correlates this request with the elicitation-complete notification defined in §20.6. [MCP]
- `url`: REQUIRED string. The URL the user should navigate to. It MUST be a valid URI [RFC3986] and MUST contain a valid URL. [MCP][RFC3986]

### §20.4 The restricted form schema

For form mode, `requestedSchema.properties` maps each field name to a `PrimitiveSchemaDefinition`. To keep the client user experience simple, this is a restricted subset of JSON Schema: a flat object whose properties are primitive types only. Complex nested structures, arrays of objects (beyond the enum array forms below), and other advanced JSON Schema features are intentionally not supported. [MCP][JSONSCHEMA]

```ts
PrimitiveSchemaDefinition =
  | StringSchema
  | NumberSchema
  | BooleanSchema
  | EnumSchema;
```

A client MAY use the schema to generate input forms, validate user input before sending, and guide the user. Each primitive type supports an OPTIONAL `default`; a client that supports defaults SHOULD pre-populate the corresponding field with the default value. [MCP]

#### StringSchema

```ts
StringSchema {
  type: "string";        // REQUIRED; literal
  title?: string;        // OPTIONAL; display label
  description?: string;  // OPTIONAL; descriptive text
  minLength?: number;    // OPTIONAL; minimum string length
  maxLength?: number;    // OPTIONAL; maximum string length
  format?: "email" | "uri" | "date" | "date-time";  // OPTIONAL; one of these literals
  default?: string;      // OPTIONAL; default value
}
```

`format`, when present, MUST be one of `"email"`, `"uri"`, `"date"`, `"date-time"`. [MCP]

#### NumberSchema

```ts
NumberSchema {
  type: "number" | "integer";  // REQUIRED; one of these literals
  title?: string;              // OPTIONAL; display label
  description?: string;        // OPTIONAL; descriptive text
  minimum?: number;            // OPTIONAL; inclusive lower bound
  maximum?: number;            // OPTIONAL; inclusive upper bound
  default?: number;            // OPTIONAL; default value
}
```

`type` MUST be `"number"` or `"integer"`. [MCP]

#### BooleanSchema

```ts
BooleanSchema {
  type: "boolean";       // REQUIRED; literal
  title?: string;        // OPTIONAL; display label
  description?: string;  // OPTIONAL; descriptive text
  default?: boolean;     // OPTIONAL; default value
}
```

#### Enum schemas

Enumerations come in single-select and multi-select forms, each available with or without per-option display titles, plus a legacy titled form. [MCP]

```ts
EnumSchema =
  | SingleSelectEnumSchema
  | MultiSelectEnumSchema
  | LegacyTitledEnumSchema;

SingleSelectEnumSchema =
  | UntitledSingleSelectEnumSchema
  | TitledSingleSelectEnumSchema;

MultiSelectEnumSchema =
  | UntitledMultiSelectEnumSchema
  | TitledMultiSelectEnumSchema;
```

##### UntitledSingleSelectEnumSchema

A single choice from a list of string values, with no separate display labels.

```ts
UntitledSingleSelectEnumSchema {
  type: "string";        // REQUIRED; literal
  title?: string;        // OPTIONAL; field title
  description?: string;  // OPTIONAL; field description
  enum: string[];        // REQUIRED; the values to choose from
  default?: string;      // OPTIONAL; default value
}
```

##### TitledSingleSelectEnumSchema

A single choice where each option carries a separate display label.

```ts
TitledSingleSelectEnumSchema {
  type: "string";        // REQUIRED; literal
  title?: string;        // OPTIONAL; field title
  description?: string;  // OPTIONAL; field description
  oneOf: Array<{         // REQUIRED; one entry per selectable option
    const: string;       // REQUIRED; the enum value
    title: string;       // REQUIRED; display label for this option
  }>;
  default?: string;      // OPTIONAL; default value (a member of the option consts)
}
```

##### UntitledMultiSelectEnumSchema

Selection of zero or more values from a list, with no separate display labels.

```ts
UntitledMultiSelectEnumSchema {
  type: "array";         // REQUIRED; literal
  title?: string;        // OPTIONAL; field title
  description?: string;  // OPTIONAL; field description
  minItems?: number;     // OPTIONAL; minimum number of selections
  maxItems?: number;     // OPTIONAL; maximum number of selections
  items: {               // REQUIRED; schema for each selected item
    type: "string";      // REQUIRED; literal
    enum: string[];      // REQUIRED; the values to choose from
  };
  default?: string[];    // OPTIONAL; default selection
}
```

##### TitledMultiSelectEnumSchema

Selection of zero or more values where each option carries a separate display label.

```ts
TitledMultiSelectEnumSchema {
  type: "array";         // REQUIRED; literal
  title?: string;        // OPTIONAL; field title
  description?: string;  // OPTIONAL; field description
  minItems?: number;     // OPTIONAL; minimum number of selections
  maxItems?: number;     // OPTIONAL; maximum number of selections
  items: {               // REQUIRED; schema for the array items
    anyOf: Array<{       // REQUIRED; one entry per selectable option
      const: string;     // REQUIRED; the enum value
      title: string;     // REQUIRED; display label for this option
    }>;
  };
  default?: string[];    // OPTIONAL; default selection
}
```

##### LegacyTitledEnumSchema

This enum form is Deprecated. Implementations SHOULD NOT adopt it for new functionality; it remains defined for interoperability. It conveys per-value display labels through a parallel `enumNames` array, which is non-standard with respect to JSON Schema 2020-12. Implementations needing per-option display labels SHOULD use `TitledSingleSelectEnumSchema`. [MCP][JSONSCHEMA][SEP-2596]

```ts
LegacyTitledEnumSchema {
  type: "string";        // REQUIRED; literal
  title?: string;        // OPTIONAL; field title
  description?: string;  // OPTIONAL; field description
  enum: string[];        // REQUIRED; the values to choose from
  enumNames?: string[];  // OPTIONAL; display names, positionally aligned with `enum`
  default?: string;      // OPTIONAL; default value
}
```

### §20.5 ElicitResult and response actions

The client returns an `ElicitResult` as the supplied input on retry (carried per §11 Multi-Round-Trip Requests).

```ts
ElicitResult {
  action: "accept" | "decline" | "cancel";  // REQUIRED; one of these literals
  content?: { [key: string]: string | number | boolean | string[] };  // OPTIONAL
}
```

Field rules:

- `action`: REQUIRED. Exactly one of the literal strings `"accept"`, `"decline"`, `"cancel"`. [MCP]
- `content`: OPTIONAL map of collected values. Present only when `action` is `"accept"` and the mode was `"form"`. Omitted for URL-mode responses and typically omitted for `"decline"` and `"cancel"`. When present, each value is a string, number, boolean, or array of strings, and the map MUST conform to the `requestedSchema` supplied in the request. [MCP]

The three actions distinguish user intent. These actions apply to both form and URL modes. [MCP]

1. `"accept"` — The user explicitly approved and submitted.
   - For form mode, `content` contains the submitted data matching `requestedSchema`.
   - For URL mode, `content` is omitted. Acceptance indicates the user has consented to the interaction; it does NOT mean the out-of-band interaction is complete. The client is not aware of the outcome until and unless the server sends the elicitation-complete notification of §20.6. [MCP]
   - A server SHOULD process the submitted data.
2. `"decline"` — The user explicitly declined the request. `content` is typically omitted. A server SHOULD handle the explicit decline (for example, by offering alternatives). [MCP]
3. `"cancel"` — The user dismissed without an explicit choice (for example, closed the dialog, clicked away, pressed Escape, or the URL failed to load). `content` is typically omitted. A server SHOULD handle the dismissal (for example, by prompting again later). [MCP]

A server MUST NOT assume an elicitation request will succeed and MUST handle the cases where the user declines or cancels, or where the client otherwise fails to process the request. [MCP]

A client SHOULD validate the supplied `content` against `requestedSchema` before sending; a server SHOULD validate received data against the schema it requested. [MCP]

### §20.6 Elicitation-complete notification (URL mode)

For URL-mode elicitation, the out-of-band interaction completes outside the protocol. A server MAY signal completion to the client by sending the notification `notifications/elicitation/complete`. This allows the client to react programmatically. [MCP]

```ts
ElicitationCompleteNotification {
  method: "notifications/elicitation/complete";  // REQUIRED; exact literal
  params: {
    elicitationId: string;  // REQUIRED; the id of the elicitation that completed
  };
}
```

The notification is a JSON-RPC notification (no `id`, no response). Its base shape and the `notifications/` naming convention are defined in §3 Base Message Format and §15 Utilities: Progress, Cancellation, Logging, and Trace Context. [JSONRPC2]

Rules:

- A server sending this notification MUST include the `elicitationId` established in the original `elicitation/create` request and MUST send it only to the client that initiated that elicitation request. [MCP]
- A client MUST ignore a notification that references an unknown or already-completed `elicitationId`. [MCP]
- A client MAY wait for this notification to automatically retry the originating request, update its user interface, or otherwise continue the interaction. [MCP]
- A client SHOULD nonetheless provide manual controls that let the user retry or cancel the original request, or otherwise resume interacting with the client, in case the notification never arrives. [MCP]

### §20.7 Security and consent

Elicitation places the user, through the client, in control of what information is shared. The following requirements apply; the general protections are stated in §28 Security Considerations.

User control:

- The client MUST provide UI that makes clear which server is requesting information. [MCP]
- The client MUST give the user clear options to decline and to cancel the request at any time, and MUST respect user privacy. [MCP]
- For form mode, the client MUST allow the user to review and modify their responses before sending — that is, review and approve, edit, decline, or cancel. [MCP]
- The client SHOULD present elicitation requests so that it is clear what information is being requested and why, SHOULD implement user approval controls, and SHOULD allow the user to decline at any time. [MCP]

Sensitive information:

- A server MUST NOT use form mode to request sensitive information such as passwords, API keys, access tokens, or payment credentials. [MCP]
- A server MUST use URL mode for interactions involving such sensitive information. General contact or profile information (such as a name, email address, or username) is not categorically prohibited; whether to request such data via form mode is at the server's discretion and subject to the user's ability to review and decline. [MCP]

Server-side binding and verification:

- A server MUST bind elicitation requests to the client and user identity. [MCP]
- A server MUST NOT rely on client-provided user identification without server-side verification, since such identification can be forged. [MCP]

Cross-user phishing (URL mode). Because a URL-mode elicitation URL can be forwarded to another party, a server MUST verify the identity of the user who opens the URL before accepting any information through the out-of-band interaction. A server MUST ensure that the user who started the elicitation request — the end user accessing the server through the MCP client — is the same user who completes the out-of-band interaction (for example, an authorization flow); otherwise an attacker who triggers an elicitation and forwards the URL to a victim could have the victim complete the flow, binding the resulting credentials or tokens to the attacker's identity and causing account takeover. A server SHOULD perform this verification by identifying the user through its authorization server (§23 Authorization) — for example, by comparing the authoritative subject (`sub`) established for the MCP session against the subject of the browser session that opened the URL — rather than trusting any identity carried in the URL. In all cases, the server MUST ensure that the mechanism it uses to determine the user's identity is resilient to attacks in which an attacker modifies the elicitation URL. [MCP][SEP-2322]

Safe URL handling (URL mode). The server-side rules ensure clients can consistently apply the client-side rules:

- A server MUST NOT include sensitive information about the end user (credentials, personally identifiable information, etc.) in the elicitation URL. [MCP]
- A server MUST NOT provide a URL that is pre-authenticated to access a protected resource, as such a URL could be used to impersonate the user by a malicious client. [MCP]
- A server SHOULD NOT include clickable URLs in any field of a form-mode elicitation request. [MCP]
- A server SHOULD use HTTPS URLs outside development environments. [MCP]

A client handling URL-mode elicitation:

- MUST NOT automatically pre-fetch the URL or any of its metadata. [MCP]
- MUST NOT open the URL without explicit user consent. [MCP]
- MUST show the full URL to the user for examination before consent, clearly displaying the target domain/host. [MCP]
- MUST open the URL in a manner that does not allow the client or LLM to inspect the page content or the user's inputs. [MCP]
- SHOULD highlight the URL's domain to mitigate subdomain spoofing, and SHOULD warn about ambiguous or suspicious URIs (for example, those containing Punycode). [MCP]
- SHOULD NOT render URLs as clickable in any field of an elicitation request, except for the `url` field of a URL-mode request, subject to the rules above. [MCP]

URL-mode elicitation is NOT a mechanism for authorizing the client's own access to the server; that is handled by the authorization model of §23 Authorization. A server MUST NOT rely on URL-mode elicitation to authorize users for itself, and MUST NOT transmit credentials obtained through URL-mode elicitation to the client. [MCP]

### §20.8 Examples

#### Form-mode elicitation as an input-required result

A server processing a client request returns an input-required result that embeds an `elicitation/create` request. The result-type discriminator marking this as input-required, and the surrounding round-trip envelope, are defined in §3 Base Message Format and §11 Multi-Round-Trip Requests; the embedded request is shown here.

```json
{
  "method": "elicitation/create",
  "params": {
    "mode": "form",
    "message": "Please provide your contact information",
    "requestedSchema": {
      "type": "object",
      "properties": {
        "name": {
          "type": "string",
          "description": "Your full name"
        },
        "email": {
          "type": "string",
          "format": "email",
          "description": "Your email address"
        },
        "age": {
          "type": "number",
          "minimum": 18,
          "description": "Your age"
        }
      },
      "required": ["name", "email"]
    }
  }
}
```

#### Client retry carrying the ElicitResult (action accept, with content)

On the retry of the originating request, the client supplies the collected `ElicitResult` as the input response (carried per §11 Multi-Round-Trip Requests). The result payload is:

```json
{
  "result": {
    "action": "accept",
    "content": {
      "name": "Monalisa Octocat",
      "email": "octocat@github.com",
      "age": 30
    }
  }
}
```

#### URL-mode elicitation request

```json
{
  "method": "elicitation/create",
  "params": {
    "mode": "url",
    "elicitationId": "550e8400-e29b-41d4-a716-446655440000",
    "url": "https://mcp.example.com/ui/set_api_key",
    "message": "Please provide your API key to continue."
  }
}
```

The client presents the URL for examination, gathers consent, opens the URL securely, and returns acceptance of the interaction (no `content` for URL mode):

```json
{
  "result": {
    "action": "accept"
  }
}
```

#### Elicitation-complete notification

When the out-of-band interaction completes, the server MAY send:

```json
{
  "jsonrpc": "2.0",
  "method": "notifications/elicitation/complete",
  "params": {
    "elicitationId": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

The `elicitationId` matches the value from the original URL-mode request, correlating the completion with that elicitation.

## §21 Deprecated Client-Provided Capabilities

The two capabilities defined in this section — Roots and Sampling — are Deprecated as a present condition; implementations SHOULD NOT adopt them for new functionality, and they remain defined here only for interoperability. Like elicitation (§20 Elicitation), neither is delivered as a server-initiated JSON-RPC request: a server requests each one by returning an input-required result, and the client supplies it by retrying the originating request — the multi-round-trip mechanism defined in §11 Multi-Round-Trip Requests. [SEP-2577][SEP-2596][SEP-2322]

### §21.1 Roots (Deprecated)

This capability is Deprecated. Implementations SHOULD NOT adopt it for new functionality; it remains defined for interoperability. [SEP-2577][SEP-2596]

#### §21.1.1 Status and Migration Guidance

The Roots capability lets a client expose filesystem "roots" — directories and files the client considers relevant — so that a server can focus its operations on them. Roots are informational guidance, not an access-control mechanism: the protocol does not enforce that a server confines its operations to the listed roots. [MCP]

This capability is Deprecated. [SEP-2577][SEP-2596] Implementations SHOULD NOT adopt it for new functionality. Implementations SHOULD instead convey relevant directories and files to a server through tool input parameters (see §16 Tools), through resource URIs (see §17 Resources), or through server configuration. The capability remains defined in this specification for interoperability with existing deployments.

The "Deprecated" status label applies to this capability as a present condition. The capability remains part of this specification, and a conforming receiver MUST continue to honor the wire contract defined here for as long as the capability is published.

#### §21.1.2 Capability Declaration and Gating

A client that supports roots-listing declares the `roots` capability. Capability declaration and the request-scoped negotiation envelope are defined in §6 Capabilities and Extensions; that envelope is carried in the reserved request-metadata key `_meta.io.modelcontextprotocol/clientCapabilities` (see §4 Request Metadata and the Stateless Model). [SEP-2575]

The `roots` capability is an object with no defined members in this revision:

```ts
interface ClientCapabilities {
  // ... other client capabilities ...

  /**
   * Present if the client supports listing roots.
   * Deprecated.
   */
  roots?: {};
}
```

Rules:

- The value of `roots`, when present, MUST be a JSON object. It has no defined members; the empty object `{}` is the canonical value. A receiver MUST ignore any unrecognized members rather than rejecting the capability.
- Presence of the `roots` key (with any object value) signals that the client supports roots-listing. Absence of the key signals that the client does not support roots-listing.
- This revision defines no `listChanged` sub-flag for the `roots` capability. A client MUST NOT rely on a `listChanged`-style change-notification mechanism for roots in this revision.
- A server MUST NOT request a roots listing from a client that has not declared the `roots` capability. If a server would otherwise request roots from a client that has not declared the capability, the server MUST proceed without roots.

#### §21.1.3 How a Roots Listing Is Requested

A roots listing is NOT delivered as a server-initiated JSON-RPC request. A server requests a roots listing by returning an input-required result, and the client supplies the listing by retrying the original request — the multi-round-trip mechanism defined in §11 Multi-Round-Trip Requests. [SEP-2322]

Specifically:

- While processing a client request (for example, a tools/call from §16 Tools), a server that needs the client's roots returns an input-required result. That result carries an input request whose `method` is the string `"roots/list"`, together with the request-state token the client must echo on retry. The exact shape of the input-required result, the input-request envelope, and the request-state token are defined in §11 Multi-Round-Trip Requests; the result-type discriminator that marks a result as input-required is the `resultType` field defined in §3 Base Message Format.
- The client gathers the requested roots and retries the original request, supplying the roots listing as the input response keyed to the `"roots/list"` input request, alongside the echoed request state. The keying of input responses to input requests on retry is defined in §11 Multi-Round-Trip Requests.
- Because the roots listing travels as an input response inside a retry of the client's own request — not as a reply to a server-initiated request — the server is not blocked waiting on a dedicated response message. If the client cannot or will not supply roots, it is not required to replay the original call with an error solely to report that fact; the multi-round-trip resolution and error semantics are defined in §11 Multi-Round-Trip Requests and §22 Error Handling and Error Codes. [MCP]

#### §21.1.4 The roots/list Input Request

The input request that a server embeds to obtain roots is shaped as follows:

```ts
interface ListRootsRequest {
  method: 'roots/list';
  params?: RequestParams;
}
```

Fields:

- `method`: REQUIRED. String. MUST be exactly `"roots/list"` (case-sensitive).
- `params`: OPTIONAL. A request-parameters object. It carries no roots-specific members; it MAY carry the common request-metadata `_meta` member. The `RequestParams` shape and the `_meta` member are defined in §14 Common Data Types and §4 Request Metadata and the Stateless Model. A receiver MUST tolerate the absence of `params`.

#### §21.1.5 The ListRootsResult and the Root Type

On retry, the client supplies a result containing the array of roots:

```ts
interface ListRootsResult {
  roots: Root[];
}

interface Root {
  /**
   * The URI identifying the root. MUST start with "file://".
   */
  uri: string;
  /**
   * An optional human-readable name for the root.
   */
  name?: string;
  _meta?: { [key: string]: unknown };
}
```

`ListRootsResult` fields:

- `roots`: REQUIRED. An array of `Root` objects. The array MAY be empty (`[]`), indicating the client exposes no roots. The array MUST be present even when empty.

`Root` fields:

- `uri`: REQUIRED. String. The URI identifying the root. In this revision it MUST be a URI whose scheme is `file`; that is, it MUST begin with `file://`. A receiver MAY reject, or ignore, a root whose `uri` does not use the `file` scheme. The `uri` value MUST be a syntactically valid URI [RFC3986].
- `name`: OPTIONAL. String. A human-readable name for the root, suitable for display or for referencing the root elsewhere in an application. When absent, no display name is implied.
- `_meta`: OPTIONAL. An object mapping string keys to arbitrary JSON values, used to attach implementation-defined metadata to the root. The reserved-key conventions for `_meta` are defined in §4 Request Metadata and the Stateless Model and §14 Common Data Types. A receiver MUST ignore `_meta` members it does not recognize.

Constraints and behavior:

- A client MUST only expose roots it intends a server to treat as in-scope, and SHOULD obtain user consent before exposing roots to a server. [MCP]
- A client SHOULD validate every root `uri` (for example, to guard against path-traversal artifacts) before exposing it. [MCP]
- A server SHOULD tolerate roots becoming unavailable after they are reported, and SHOULD validate any paths it derives against the reported roots; a server MUST NOT assume the protocol enforces root boundaries on its behalf. [MCP]

#### §21.1.6 Example

A server, while processing a client request, returns an input-required result carrying the `roots/list` input request. The framing of the input-required result is defined in §11 Multi-Round-Trip Requests; the embedded input request is:

```json
{
  "method": "roots/list"
}
```

On retry, the client supplies the corresponding `ListRootsResult` as the input response for that input request:

```json
{
  "roots": [
    {
      "uri": "file:///home/user/projects/myproject",
      "name": "My Project"
    },
    {
      "uri": "file:///home/user/repos/backend",
      "name": "Backend Repository"
    }
  ]
}
```

A client that exposes no roots supplies an empty array:

```json
{
  "roots": []
}
```

### §21.2 Sampling (Deprecated)

This capability is Deprecated. Implementations SHOULD NOT adopt it for new functionality; it remains defined for interoperability. [SEP-2577][SEP-2596]

#### §21.2.1 Status and Scope

Sampling lets a server obtain a language-model completion ("sampling", "generation", or "completion") by delegating the model call to the client. The client retains control over which model runs, over model access and permissions, and over whether the call proceeds at all, so a server can leverage model capabilities without holding any model-provider credentials of its own. [MCP]

This capability is Deprecated. Implementations SHOULD NOT adopt it for new functionality, and SHOULD instead integrate directly with a model provider. It remains defined for interoperability. [SEP-2577][SEP-2596] As a Deprecated feature it remains in this specification for the period required by the lifecycle policy described in §27 Feature Lifecycle and Deprecation before it becomes eligible for removal. [SEP-2596]

#### §21.2.2 Delivery Through a Multi-Round-Trip Request

A sampling request is NOT a server-initiated JSON-RPC request. A server requests a completion from the client by returning an input-required result that carries an input request whose method name is the string `sampling/createMessage`, as part of the multi-round-trip mechanism of §11 Multi-Round-Trip Requests. The client runs the model, obtains the completion, and supplies it back to the server by retrying the original request with the completion attached, following the carry-forward and retry rules of §11 Multi-Round-Trip Requests. [MCP][SEP-2322]

Because the server is not blocked awaiting a JSON-RPC response, the input-required pattern has a distinct failure mode: if an error occurs, or the user declines the sampling request, the client does not retry the originating request with an error, and the server does not wait for any sampling response. Error reporting otherwise follows §22 Error Handling and Error Codes. [MCP]

#### §21.2.3 Client Capability

A client that supports sampling declares the `sampling` capability during initialization (capability declaration and gating are defined in §6 Capabilities and Extensions). The capability is an object with the following OPTIONAL sub-capability members. [MCP][SEP-2575]

```ts
interface ClientSamplingCapability {
  /**
   * Present if the client supports tool use during sampling, via the
   * `tools` and `toolChoice` request fields. An empty object indicates
   * support with no further settings.
   */
  tools?: { [key: string]: unknown };

  /**
   * Present if the client supports context inclusion via the
   * `includeContext` request field. An empty object indicates support
   * with no further settings.
   * This sub-capability is Deprecated. [SEP-2596]
   */
  context?: { [key: string]: unknown };
}
```

Gating rules:

- A server MUST NOT send a tool-enabled sampling request (one that includes `tools` or `toolChoice`) to a client that has not declared `sampling.tools`. A client MUST return an error if a sampling input request includes `tools` or `toolChoice` but the client did not declare `sampling.tools`. [MCP]
- If the client does not declare `sampling.context`, a server SHOULD use only `includeContext: "none"` or omit the field. The `context` sub-capability is Deprecated. [SEP-2596]

The minimum declaration is an empty object:

```json
{
  "capabilities": {
    "sampling": {}
  }
}
```

With tool-use support:

```json
{
  "capabilities": {
    "sampling": {
      "tools": {}
    }
  }
}
```

With context-inclusion support (the `context` sub-capability is Deprecated):

```json
{
  "capabilities": {
    "sampling": {
      "context": {}
    }
  }
}
```

#### §21.2.4 Request Parameters

The input request method name is the exact string `sampling/createMessage`. Its parameters are a `CreateMessageRequestParams` object.

```ts
interface CreateMessageRequest {
  method: "sampling/createMessage";
  params: CreateMessageRequestParams;
}

interface CreateMessageRequestParams {
  messages: SamplingMessage[];
  modelPreferences?: ModelPreferences;
  systemPrompt?: string;
  includeContext?: "none" | "thisServer" | "allServers";
  temperature?: number;
  maxTokens: number;
  stopSequences?: string[];
  metadata?: { [key: string]: unknown };
  tools?: Tool[];
  toolChoice?: ToolChoice;
}
```

Field semantics:

- `messages` (REQUIRED, `SamplingMessage[]`): The conversation to sample from, ordered oldest to newest. Each element is a `SamplingMessage` (defined below). The list of messages in a sampling request SHOULD NOT be retained between separate requests. [MCP]
- `modelPreferences` (OPTIONAL, `ModelPreferences`): The server's advisory preferences for which model to select. The client MAY ignore these preferences. See "Model Preferences" below. [MCP]
- `systemPrompt` (OPTIONAL, `string`): A system prompt the server wants to use for sampling. The client MAY modify or ignore this field without communicating the change to the server. [MCP]
- `includeContext` (OPTIONAL, enum): A request to include context from one or more connected servers, to be attached to the prompt. Exact values:
  - `"none"`: No additional context. This is the default when the field is omitted.
  - `"thisServer"`: Include context from the requesting server. This value is Deprecated. [SEP-2596]
  - `"allServers"`: Include context from all connected servers. This value is Deprecated. [SEP-2596]

  Servers SHOULD omit this field or use `"none"`, and SHOULD use the Deprecated values only if the client declared `sampling.context`. The client MAY modify or ignore this field without communicating the change to the server; for example, a client MAY constrain its response when honoring the field would require sharing sensitive information with a server. [MCP][SEP-2596]
- `temperature` (OPTIONAL, `number`): Controls randomness; higher values produce more randomness and lower values more deterministic output. The valid range depends on the model provider. The client MAY modify or ignore this field. [MCP]
- `maxTokens` (REQUIRED, `number`): The requested maximum number of tokens to sample, to prevent runaway completions. The client MAY sample fewer tokens than the requested maximum, but the client MUST respect this parameter as an upper bound. [MCP]
- `stopSequences` (OPTIONAL, `string[]`): Sequences that, when generated, stop generation. The client MAY modify or ignore this field. [MCP]
- `metadata` (OPTIONAL, object): Provider-specific parameters passed through to the model provider. The format is provider-specific. The client MAY modify or ignore this field. [MCP]
- `tools` (OPTIONAL, `Tool[]`): Tools the model MAY use during generation, each using the `Tool` shape defined in §16 Tools. These tool definitions are scoped to this sampling request and need not correspond to any registered server tool. A client MUST return an error if this field is provided but the client did not declare `sampling.tools`. [MCP]
- `toolChoice` (OPTIONAL, `ToolChoice`): Controls how the model uses tools. The default when omitted is `{ "mode": "auto" }`. A client MUST return an error if this field is provided but the client did not declare `sampling.tools`. [MCP]

#### §21.2.5 Tool Choice

```ts
interface ToolChoice {
  mode?: "auto" | "required" | "none";
}
```

`mode` (OPTIONAL, enum) controls the model's tool-use behavior:

- `"auto"`: The model decides whether to use tools. This is the default.
- `"required"`: The model MUST use at least one tool before completing.
- `"none"`: The model MUST NOT use any tools.

[MCP]

#### §21.2.6 Messages and Content Blocks

```ts
type Role = "user" | "assistant";

interface SamplingMessage {
  role: Role;
  content: SamplingMessageContentBlock | SamplingMessageContentBlock[];
  _meta?: { [key: string]: unknown };
}

type SamplingMessageContentBlock =
  | TextContent
  | ImageContent
  | AudioContent
  | ToolUseContent
  | ToolResultContent;
```

- `role` (REQUIRED, enum): The message role, either `"user"` or `"assistant"`. [MCP]
- `content` (REQUIRED): Either a single content block or an array of content blocks. A single block is used when the message has exactly one block; an array is used when it has one or more blocks (for example, multiple tool uses or mixed content). [MCP]
- `_meta` (OPTIONAL, object): Reserved metadata. The reserved-key namespace conventions are defined in §4 Request Metadata and the Stateless Model.

The `TextContent`, `ImageContent`, and `AudioContent` block types are defined in §14 Common Data Types; refer to that section for their full field definitions. The two sampling-specific block types follow.

```ts
interface ToolUseContent {
  type: "tool_use";
  id: string;
  name: string;
  input: { [key: string]: unknown };
  _meta?: { [key: string]: unknown };
}
```

`ToolUseContent` is a request from the assistant to call a tool.

- `type` (REQUIRED): The literal discriminator string `"tool_use"`.
- `id` (REQUIRED, `string`): A unique identifier for this tool use, used to match tool results to their corresponding tool uses.
- `name` (REQUIRED, `string`): The name of the tool to call.
- `input` (REQUIRED, object): The arguments to pass to the tool, conforming to the tool's input schema.
- `_meta` (OPTIONAL, object): Reserved metadata about the tool use. Clients SHOULD preserve this field when including tool uses in subsequent sampling requests, to enable caching optimizations (see §13 Response Caching).

[MCP]

```ts
interface ToolResultContent {
  type: "tool_result";
  toolUseId: string;
  content: ContentBlock[];
  structuredContent?: unknown;
  isError?: boolean;
  _meta?: { [key: string]: unknown };
}
```

`ToolResultContent` is the result of a tool use, provided by the user back to the assistant.

- `type` (REQUIRED): The literal discriminator string `"tool_result"`.
- `toolUseId` (REQUIRED, `string`): The `id` of the `ToolUseContent` this result corresponds to. This MUST match the `id` from a previous `ToolUseContent`.
- `content` (REQUIRED, `ContentBlock[]`): The unstructured result content, using the content-block array form defined for tool results in §16 Tools; it MAY include text, images, audio, resource links, and embedded resources.
- `structuredContent` (OPTIONAL, any JSON value): A structured result value. It MAY be any JSON value (object, array, string, number, boolean, or null). If the tool defined an output schema (see §16 Tools), this SHOULD conform to that schema.
- `isError` (OPTIONAL, `boolean`): Whether the tool use resulted in an error. If `true`, `content` typically describes the error. The default when omitted is `false`.
- `_meta` (OPTIONAL, object): Reserved metadata about the tool result. Clients SHOULD preserve this field when including tool results in subsequent sampling requests, to enable caching optimizations (see §13 Response Caching).

[MCP]

#### §21.2.7 Message Content Constraints

When a message with `role: "user"` contains tool results, that message MUST contain ONLY content blocks of type `"tool_result"`. Mixing `"tool_result"` blocks with any other content type (`"text"`, `"image"`, `"audio"`) in the same message is NOT allowed. This constraint ensures compatibility with provider interfaces that use a dedicated role for tool results. [MCP]

Every `assistant` message that contains one or more `ToolUseContent` blocks MUST be followed immediately by a `user` message consisting entirely of `ToolResultContent` blocks, with each tool use (carrying `id: $id`) matched by a corresponding tool result (carrying `toolUseId: $id`), before any other message. This guarantees that tool uses are always resolved before the conversation continues. The protocol permits multiple tool uses to be requested in parallel (an array of `ToolUseContent` blocks). [MCP]

#### §21.2.8 Result

The completion is delivered back to the server (by retrying per §11 Multi-Round-Trip Requests) as a `CreateMessageResult`.

```ts
interface CreateMessageResult {
  role: Role;
  content: SamplingMessageContentBlock | SamplingMessageContentBlock[];
  model: string;
  stopReason?: "endTurn" | "stopSequence" | "maxTokens" | "toolUse" | string;
  resultType: string;
  _meta?: { [key: string]: unknown };
}
```

- `role` (REQUIRED, enum): The role of the produced message, `"user"` or `"assistant"`. A completion is normally `"assistant"`. [MCP]
- `content` (REQUIRED): The produced content. This is a single sampling content block when the response contains exactly one block (such as a single text response), or an array of sampling content blocks when the response contains one or more blocks (such as multiple tool uses or mixed content). Tool-use requests are returned in the `"assistant"` role as `ToolUseContent`. [MCP]
- `model` (REQUIRED, `string`): The name of the model that generated the message. [MCP]
- `stopReason` (OPTIONAL, string): The reason sampling stopped, if known. This field is an open string to allow provider-specific values; the standard values are:
  - `"endTurn"`: Natural end of the assistant's turn (the participant is yielding the conversation).
  - `"stopSequence"`: One of the requested `stopSequences` was encountered.
  - `"maxTokens"`: The maximum token limit was reached.
  - `"toolUse"`: The model wants to use one or more tools.

  Implementations MAY provide additional arbitrary values. [MCP]
- `resultType` (REQUIRED, `string`): The result-type discriminator. Its meaning, permitted values, and placement are defined in §3 Base Message Format.
- `_meta` (OPTIONAL, object): Reserved metadata. The reserved-key namespace conventions are defined in §4 Request Metadata and the Stateless Model.

#### §21.2.9 Model Preferences

```ts
interface ModelPreferences {
  hints?: ModelHint[];
  costPriority?: number;
  speedPriority?: number;
  intelligencePriority?: number;
}

interface ModelHint {
  name?: string;
}
```

Model selection requires abstraction because servers and clients may use different providers with distinct model offerings; a server cannot reliably name an exact model the client can run. `ModelPreferences` lets a server express its priorities and optional hints. All preferences are advisory: the client MAY ignore them, and it is the client's (or host's) responsibility to decide how to interpret and balance them and to make the final model selection. [MCP]

- `hints` (OPTIONAL, `ModelHint[]`): Hints to guide model selection. If multiple hints are specified, the client MUST evaluate them in order, taking the first match. The client SHOULD prioritize hints over the numeric priorities, but MAY still use the priorities to choose among ambiguous matches. [MCP]
- `costPriority` (OPTIONAL, `number`, range 0 to 1 inclusive): How much to prioritize minimizing cost. `0` means cost is not important; `1` means cost is the most important factor. [MCP]
- `speedPriority` (OPTIONAL, `number`, range 0 to 1 inclusive): How much to prioritize sampling speed (low latency). `0` means speed is not important; `1` means speed is the most important factor. [MCP]
- `intelligencePriority` (OPTIONAL, `number`, range 0 to 1 inclusive): How much to prioritize intelligence and capability. `0` means intelligence is not important; `1` means intelligence is the most important factor. [MCP]

For `ModelHint`:

- `name` (OPTIONAL, `string`): A hint for a model name. The client SHOULD treat this as a substring of a model name (for example, `claude-3-5-sonnet` matches `claude-3-5-sonnet-20241022`; `sonnet` matches `claude-3-5-sonnet-20241022` and `claude-3-sonnet-20240229`; `claude` matches any Claude model). The client MAY map the string to a different provider's model name or a different model family that fills a similar niche. Keys other than `name` are unspecified and are left to the client to interpret. [MCP]

#### §21.2.10 Consent and Safety

The client (or its host) MUST keep a human in the loop and MUST give the user the ability to deny a sampling request. Before sampling begins, the client SHOULD present the request so the user can review, edit, or reject the prompt; after the model produces a completion, the client SHOULD present the result so the user can review, edit, or reject it before the server is allowed to see it. The client MAY modify or omit `systemPrompt`, `includeContext`, `temperature`, `stopSequences`, and `metadata` as part of this control. Clients SHOULD implement rate limiting, both parties SHOULD validate message content, and both parties MUST handle sensitive data appropriately. When tools are used in sampling, both parties SHOULD implement iteration limits for tool loops. These obligations are part of, and elaborated in, §28 Security Considerations. [MCP]

#### §21.2.11 Examples

A `sampling/createMessage` input request returned inside an input-required result (the input-required envelope structure is defined in §11 Multi-Round-Trip Requests; only the carried sampling input request is shown here):

```json
{
  "method": "sampling/createMessage",
  "params": {
    "messages": [
      {
        "role": "user",
        "content": {
          "type": "text",
          "text": "What is the capital of France?"
        }
      }
    ],
    "modelPreferences": {
      "hints": [
        { "name": "claude-3-sonnet" },
        { "name": "claude" }
      ],
      "costPriority": 0.3,
      "speedPriority": 0.5,
      "intelligencePriority": 0.8
    },
    "systemPrompt": "You are a helpful assistant.",
    "temperature": 0.1,
    "maxTokens": 100
  }
}
```

The `CreateMessageResult` supplied back to the server on retry (the `resultType` discriminator is defined in §3 Base Message Format):

```json
{
  "role": "assistant",
  "content": {
    "type": "text",
    "text": "The capital of France is Paris."
  },
  "model": "claude-3-sonnet-20240307",
  "stopReason": "endTurn",
  "resultType": "complete"
}
```

A tool-use exchange. First, a tool-enabled sampling input request:

```json
{
  "method": "sampling/createMessage",
  "params": {
    "messages": [
      {
        "role": "user",
        "content": {
          "type": "text",
          "text": "What's the weather like in Paris?"
        }
      }
    ],
    "tools": [
      {
        "name": "get_weather",
        "description": "Get current weather for a city",
        "inputSchema": {
          "type": "object",
          "properties": {
            "city": { "type": "string", "description": "City name" }
          },
          "required": ["city"]
        }
      }
    ],
    "toolChoice": { "mode": "auto" },
    "maxTokens": 1000
  }
}
```

The client returns a `CreateMessageResult` containing `ToolUseContent` with `stopReason: "toolUse"`:

```json
{
  "role": "assistant",
  "content": [
    {
      "type": "tool_use",
      "id": "call_abc123",
      "name": "get_weather",
      "input": { "city": "Paris" }
    }
  ],
  "model": "claude-3-sonnet-20240307",
  "stopReason": "toolUse",
  "resultType": "complete"
}
```

The server executes the tool and issues a follow-up sampling input request whose `messages` append the assistant's tool use and a `user` message consisting entirely of the matching `ToolResultContent`:

```json
{
  "method": "sampling/createMessage",
  "params": {
    "messages": [
      {
        "role": "user",
        "content": { "type": "text", "text": "What's the weather like in Paris?" }
      },
      {
        "role": "assistant",
        "content": [
          {
            "type": "tool_use",
            "id": "call_abc123",
            "name": "get_weather",
            "input": { "city": "Paris" }
          }
        ]
      },
      {
        "role": "user",
        "content": [
          {
            "type": "tool_result",
            "toolUseId": "call_abc123",
            "content": [
              { "type": "text", "text": "Weather in Paris: 18°C, partly cloudy" }
            ]
          }
        ]
      }
    ],
    "tools": [
      {
        "name": "get_weather",
        "description": "Get current weather for a city",
        "inputSchema": {
          "type": "object",
          "properties": { "city": { "type": "string" } },
          "required": ["city"]
        }
      }
    ],
    "maxTokens": 1000
  }
}
```

# Part VI — Errors and Authorization

## §22 Error Handling and Error Codes

This section defines how receivers signal that a request could not be processed, the complete registry of error codes, the precise wire shapes of error objects, and the boundary between protocol-level errors and feature-level error results. All messages are JSON-RPC 2.0 messages [JSONRPC2][MCP]. Numeric codes, field names, and data shapes are case-sensitive and are reproduced exactly as they appear on the wire.

### §22.1 Error Response Shape

An error response is a JSON-RPC response that reports failure rather than success. It is one of the two response forms defined in §3 Base Message Format. It carries the `id` of the request it answers and an `error` object in place of a `result`. A single response object MUST contain either a `result` member or an `error` member, never both and never neither.

```ts
interface JSONRPCErrorResponse {
  jsonrpc: "2.0";
  id?: string | number;   // MUST equal the corresponding request id; see §22.6 for the id-omission rule
  error: Error;
}
```

The `error` object uses the canonical `Error` type defined in §3.8 (`code`, `message`, OPTIONAL `data`). `JSONRPCErrorResponse` is the §3.5.2 error-response envelope restated here for reference.

Field rules:

- `jsonrpc` — REQUIRED. MUST be the exact string `"2.0"`.
- `id` — REQUIRED in the normal case and MUST equal the `id` of the request being answered. It MAY be omitted only when the request `id` could not be determined; see §22.6. Notifications never receive any response, error or otherwise.
- `error.code` — REQUIRED. MUST be an integer (it MAY be negative). It identifies the error condition from the registries in §22.2 and §22.3, or an extension-defined code per §22.7.
- `error.message` — REQUIRED. A short, human-readable description. The `message` is informational and MUST NOT be parsed by receivers to determine the error condition; the `code` is authoritative.
- `error.data` — OPTIONAL. Additional information whose structure is defined by the sender or by the specific error code. For the codes in §22.3 the shape of `data` is normative and is given there.

The `result` form (with the `resultType` discriminator) and the overall request/response correlation model are defined in §3 Base Message Format.

### §22.2 Standard Error Code Registry

The following codes are the standard JSON-RPC codes [JSONRPC2] and carry their standard meanings in MCP [MCP]. Every receiver MUST use these codes for the conditions described.

| Code | Name | Meaning |
| --- | --- | --- |
| `-32700` | Parse error | Invalid JSON was received. The receiver could not parse the byte stream of the message as JSON text [RFC8259]. |
| `-32600` | Invalid Request | The received payload is valid JSON but is not a valid JSON-RPC request object (for example, a missing or wrongly typed `jsonrpc` or `method` member). |
| `-32601` | Method not found | The requested method does not exist or is not available on the receiver. |
| `-32602` | Invalid params | The method's parameters are invalid or malformed. See §22.4 for the MCP conditions that map to this code. |
| `-32603` | Internal error | The receiver encountered an unexpected condition that prevented it from fulfilling an otherwise well-formed request. |

`-32601` (Method not found) applies both to a genuinely unknown method and to a method that is gated behind a server capability the server did not advertise. For example, a server that did not advertise the `prompts` capability and receives `prompts/list` MUST respond with `-32601`. The complementary condition — a request that requires a *client* capability the client did not declare — is NOT signaled with `-32601`; it is signaled with `-32003` (see §22.3). Capability advertisement and negotiation are defined in §6 Capabilities and Extensions.

### §22.3 Protocol-Specific Error Codes

MCP defines the following additional integer codes [MCP]. For each, the `error.data` shape is normative and MUST be populated as specified. These codes pertain to per-request protocol metadata and version negotiation, which are defined in §4 Request Metadata and the Stateless Model and §5 Protocol Revision, Version Negotiation, and Discovery.

#### §22.3.1 `-32003` MissingRequiredClientCapability

Returned when processing a request requires a client capability that the client did not declare in its per-request capabilities (the `clientCapabilities` carried in request metadata; see §4 Request Metadata and the Stateless Model). A server MUST NOT rely on a capability the client has not declared; when such a capability is required, the server MUST return this error rather than proceeding. The `data.requiredCapabilities` member lists the capabilities the server requires from the client to process the request. Capability negotiation rules are in §6 Capabilities and Extensions; version negotiation context is in §5 Protocol Revision, Version Negotiation, and Discovery.

```ts
interface MissingRequiredClientCapabilityError {
  jsonrpc: "2.0";
  id?: string | number;
  error: {
    code: -32003;
    message: string;
    data: {
      // The capabilities the server requires from the client to process this request.
      requiredCapabilities: ClientCapabilities;   // shape defined in §6 Capabilities and Extensions
    };
  };
}
```

#### §22.3.2 `-32004` UnsupportedProtocolVersion

Returned when the protocol revision carried in the request's metadata (see §4 Request Metadata and the Stateless Model) is unknown to or not supported by the server. The `data.supported` member lists the revisions the server supports; the client SHOULD choose a mutually supported revision from this list and retry. The `data.requested` member echoes the revision the client requested. Protocol revision strings and negotiation are defined in §5 Protocol Revision, Version Negotiation, and Discovery.

```ts
interface UnsupportedProtocolVersionError {
  jsonrpc: "2.0";
  id?: string | number;
  error: {
    code: -32004;
    message: string;
    data: {
      // Protocol revisions the server supports. The client SHOULD pick a mutually
      // supported revision from this list and retry.
      supported: string[];
      // The protocol revision that the client requested.
      requested: string;
    };
  };
}
```

### §22.4 Canonical Uses of `-32602` (Invalid params)

In MCP, `-32602` (Invalid params) is the canonical code for a well-formed request whose parameters fail validation. Receivers MUST use `-32602` for at least the following conditions:

- **Unknown tool name.** A `tools/call` naming a tool the server does not expose. See §16 Tools.
- **Invalid tool arguments.** A `tools/call` whose `arguments` do not satisfy the tool's declared input schema (missing required argument, wrong type, failed constraint). See §16 Tools.
- **Unknown prompt name or missing required prompt argument.** A `prompts/get` naming an unknown prompt, or omitting a required argument. See §18 Prompts.
- **Unknown resource template name.** A request referencing a resource template the server does not expose. See §17 Resources.
- **Invalid or expired pagination cursor.** A request carrying a `cursor` value the server cannot honor. See §12 Pagination.
- **Resource not found.** A `resources/read` for a URI that does not exist MUST return `-32602`; the `error.data` SHOULD include a `uri` member identifying the requested resource [SEP-2164]. A server MUST NOT signal a non-existent resource by returning an empty `contents` array, because an empty array is ambiguous (it could mean the resource exists but is empty). See §17 Resources.

Other validation failures of well-formed parameters likewise use `-32602` (for example, an invalid logging level supplied to a logging request; see §15 Utilities: Progress, Cancellation, Logging, and Trace Context). When the failure is not a parameter-validation failure but an unexpected server-side condition, the server SHOULD return `-32603` (Internal error) instead.

### §22.5 Protocol Errors Versus Feature-Level Error Results

There are two distinct mechanisms for reporting that something went wrong, and senders MUST choose the correct one:

1. **JSON-RPC error response (this section).** An `error` object signals that the request itself could not be processed: it was malformed, named an unknown method, carried invalid parameters, required an undeclared capability, used an unsupported protocol revision, or hit an internal failure. The request produced no successful `result`.

2. **Feature-level error result.** Some operations complete successfully at the protocol layer but report a domain-level failure inside the `result`. The primary case is tool execution: when a tool runs but its execution fails (for example, an upstream API returns an error, or the tool's own logic fails), the server MUST return a normal successful response whose `result` has `isError` set to `true` and whose content describes the failure, rather than a JSON-RPC `error`. This lets the failure be surfaced to the language model for inspection and recovery. The `isError` mechanism and tool-result shape are defined in §16 Tools.

The distinction is normative: a `tools/call` for an unknown tool, or with arguments that fail schema validation, is a protocol error and MUST be reported with `-32602` (§22.4); a `tools/call` that reaches and runs the tool but the tool's work fails MUST be reported as a successful result with `isError: true` (§16 Tools). Servers MUST NOT conflate the two — they MUST NOT return a JSON-RPC `error` for an ordinary tool-execution failure, and MUST NOT return `isError: true` for a request that could not be dispatched at all.

### §22.6 Transport Error Mapping and Malformed Messages

Error responses are transport-independent JSON-RPC messages and apply on every transport (see §7 Transport Model, §8 The stdio Transport, §9 The Streamable HTTP Transport).

**HTTP status mapping.** On the Streamable HTTP transport, error responses are delivered with the HTTP status mapping defined in §9 The Streamable HTTP Transport. In particular, `-32003` (MissingRequiredClientCapability) and `-32004` (UnsupportedProtocolVersion) MUST be returned with HTTP status `400 Bad Request`. Transport-level validation failures of an HTTP request that concern the routing headers — a missing, malformed, or mismatched `MCP-Protocol-Version`, `Mcp-Method`, `Mcp-Name`, or parameter header — MUST be reported with HTTP status `400 Bad Request` and the `-32001` (`HeaderMismatch`) error defined in §9 The Streamable HTTP Transport (`-32001` lies in the implementation-defined server-error range `-32000` to `-32099`). A structurally invalid HTTP request that is not a routing-header failure uses `-32600` (Invalid Request), and a well-formed request whose required per-request metadata is missing or invalid (the per-request metadata fields are defined in §4 Request Metadata and the Stateless Model) uses `-32602` (Invalid params). The authoritative status-mapping table is in §9 The Streamable HTTP Transport; this section does not override it.

**Malformed messages (parse errors).** When a receiver cannot parse an incoming byte stream as JSON, it MUST treat the condition as a parse error and use code `-32700`. When the received JSON is parseable but is not a valid request object, the receiver MUST use `-32600` (Invalid Request).

**The `id` rule for unparseable input.** An error response normally MUST carry the same `id` as the request it answers (§22.1, §3 Base Message Format). When the request `id` cannot be determined — for example, the payload could not be parsed, or it lacked a usable `id` — the error response MAY omit `id` (or, where a value is structurally required by the transport, send `id` as `null`). This is the only circumstance in which an error response's `id` need not match a request `id`. Notifications (messages without an `id`) never receive any response; a receiver MUST NOT emit an error response to a notification.

### §22.7 Extensibility and Unknown Codes

Implementations and extensions MAY define additional numeric error codes for conditions not covered by §22.2 and §22.3. Such codes:

- MUST be integers.
- MUST NOT collide with any code defined in this specification: the standard codes (`-32700`, `-32600`, `-32601`, `-32602`, `-32603`), the protocol-specific codes (`-32003`, `-32004`), or the Streamable HTTP transport code `-32001` (`HeaderMismatch`, defined in §9 The Streamable HTTP Transport, within the implementation-defined server-error range `-32000` to `-32099`).
- SHOULD carry descriptive structured information in `error.data` so receivers can act on the condition programmatically.

Receivers MUST tolerate unknown error codes: an error response with a `code` the receiver does not recognize MUST be treated as a failed request and surfaced (for example, logged or propagated to the caller) using `error.message` and `error.data`, rather than being rejected as malformed. The extension mechanism for negotiating extension-defined behavior is described in §24 The Extension Mechanism.

### §22.8 Examples

A `-32602` error for invalid (unknown) tool parameters:

```json
{
  "jsonrpc": "2.0",
  "id": 7,
  "error": {
    "code": -32602,
    "message": "Unknown tool: get_weatherr",
    "data": {
      "toolName": "get_weatherr"
    }
  }
}
```

A `-32602` resource-not-found error whose `data` includes the requested `uri` [SEP-2164]:

```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "error": {
    "code": -32602,
    "message": "Resource not found",
    "data": {
      "uri": "file:///nonexistent.txt"
    }
  }
}
```

A `-32601` method-not-found error for a method gated behind an unadvertised capability:

```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "error": {
    "code": -32601,
    "message": "Method not found: prompts/list"
  }
}
```

A `-32004` UnsupportedProtocolVersion error with normative `data`:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "error": {
    "code": -32004,
    "message": "Unsupported protocol version",
    "data": {
      "supported": ["2026-07-28"],
      "requested": "1999-01-01"
    }
  }
}
```

A `-32003` MissingRequiredClientCapability error with normative `data`:

```json
{
  "jsonrpc": "2.0",
  "id": 9,
  "error": {
    "code": -32003,
    "message": "Required client capability not declared",
    "data": {
      "requiredCapabilities": {
        "elicitation": {}
      }
    }
  }
}
```

## §23 Authorization

Authorization secures HTTP-based MCP transports. The MCP server is an OAuth 2.0 protected resource fronted by one or more separate authorization servers; clients perform discovery, obtain audience-bound access tokens scoped to the specific MCP server, and present those tokens as bearer credentials on every request.

### §23.1 Authorization Model and Applicability

This section defines how a client obtains and presents OAuth 2.0 credentials to access a protected MCP server [MCP][RFC6749]. Authorization is OPTIONAL for an MCP implementation. When an implementation supports it, the rules in this section apply.

Authorization as defined here applies ONLY to HTTP-based transports, that is, to the transport defined in §9 The Streamable HTTP Transport. The stdio transport defined in §8 The stdio Transport MUST NOT use this flow; a client launching a server over stdio conveys any required credentials out of band through the child process environment. Implementations using any other transport MUST follow established security best practices for that transport and are outside the scope of this section.

The flow is layered on standard OAuth 2.0 and OpenID Connect mechanisms. Every rule, parameter, header, metadata field, and `.well-known` path needed to implement it is stated inline below; citation markers identify provenance only.

An authorization server MUST implement OAuth 2.1 with appropriate security measures for both confidential and public clients [OAUTH21]. The MCP server acts as an OAuth 2.1 resource server and the MCP client acts as an OAuth 2.1 client [OAUTH21]. Access-token handling on requests to the MCP server MUST conform to OAuth 2.1 resource-request requirements, including the bearer-header and audience-validation rules stated in §23.8 Access Token Usage and §23.6 Resource Indicators and Audience Binding [OAUTH21][RFC6750][RFC8707].

Beyond the OAuth 2.0 framework defined here, a client and a server MAY negotiate their own custom authentication and authorization strategy. Such a strategy is outside the scope of this section, and an implementation that uses one is still bound by every other applicable requirement of this specification. [MCP]

#### Roles

The following roles participate. Each MUST behave as specified:

- **MCP server (OAuth 2.1 resource server).** The MCP server is an OAuth 2.1 resource server. It accepts protected requests bearing access tokens, validates them, and serves or rejects the request accordingly [OAUTH21][RFC6749][RFC6750]. The MCP server publishes protected-resource metadata (see §23.2 Protected Resource Metadata Discovery) that names the authorization servers it trusts.
- **OAuth 2.1 authorization server(s).** Authorization is performed by one or more OAuth 2.1 authorization servers that authenticate the user (when necessary) and issue access tokens for use at the MCP server [OAUTH21][RFC6749]. An authorization server MAY be co-hosted with the MCP server or operated as a separate entity. A protected-resource metadata document MAY list more than one authorization server; each is an independent authorization server, and selecting which one to use is the client's responsibility.
- **MCP client (OAuth 2.1 client).** The client is the OAuth 2.1 client acting on behalf of the resource owner (the user). It performs discovery, obtains an access token, and presents that token on each request to the MCP server [OAUTH21][RFC6749].

When multiple authorization servers are listed, client identifiers and credentials are unique to the authorization server that issued them [RFC6749]. A client MUST maintain separate registration state (client credentials, tokens) per authorization server, keyed by that authorization server's `issuer` identifier, and MUST NOT assume credentials valid for one authorization server are accepted by another [SEP-2352]. When the authorization server indicated by the MCP server's protected-resource metadata changes, the client MUST NOT reuse credentials registered with a different authorization server and MUST re-register or re-discover against the new one [SEP-2352].

#### Canonical Resource Identifier of an MCP Server

The **canonical resource identifier** of an MCP server is the URI a client uses to identify that server as the audience of an access token. It is used as the value of the `resource` parameter in OAuth requests (see §23.6 Resource Indicators and Audience Binding) and as the `resource` value in the server's protected-resource metadata [RFC8707][RFC9728].

Construction and constraints:

- The canonical resource identifier MUST be the MCP server's endpoint URL.
- It MUST be an absolute URI with the `https` scheme (or `http` only for loopback/local development) and MUST NOT contain a fragment component [RFC3986].
- The canonical form uses a lowercase scheme and host. Receivers SHOULD accept uppercase scheme and host components for robustness.
- The client SHOULD use the most specific URI it can for the server it intends to access. A path component MUST be included when it is necessary to identify an individual MCP server at the host.
- For interoperability, an implementation SHOULD use the form WITHOUT a trailing slash unless the trailing slash is semantically significant for the resource.

Valid canonical resource identifiers:

```
https://mcp.example.com/mcp
https://mcp.example.com
https://mcp.example.com:8443
https://mcp.example.com/server/mcp
```

Invalid canonical resource identifiers:

```
mcp.example.com                       (missing scheme)
https://mcp.example.com#fragment      (contains a fragment)
```

#### Unauthorized Response

When a request to the MCP server lacks valid authorization, the MCP server MUST respond with HTTP status `401 Unauthorized` and MUST include a `WWW-Authenticate` header that uses the `Bearer` scheme and directs the client to the protected-resource metadata [RFC6750][RFC9728]. The header:

- MUST include the `resource_metadata` parameter whose value is the absolute URI of the MCP server's protected-resource metadata document.
- SHOULD include a `scope` parameter listing the scopes required to access the resource, so the client can request appropriate scopes during authorization [RFC6750]. The challenged scope set is authoritative for the current operation; a client MUST treat it as the scopes required to satisfy the request and MUST NOT assume any subset/superset relationship between it and the `scopes_supported` value from protected-resource metadata.

This `401` is distinct from the JSON-RPC error codes in §22 Error Handling and Error Codes; it is an HTTP-layer response of the transport in §9 The Streamable HTTP Transport and carries no JSON-RPC error body requirement.

Example unauthorized response:

```http
HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer resource_metadata="https://mcp.example.com/.well-known/oauth-protected-resource",
                         scope="files:read"
```

A client MUST be able to parse `WWW-Authenticate` headers and react to `401 Unauthorized` responses from the MCP server.

##### Insufficient-Scope Response

When a request carries a valid token that lacks the scope required for the operation, the MCP server SHOULD respond with HTTP status `403 Forbidden` and a `WWW-Authenticate` header using the `Bearer` scheme with these parameters [RFC6750]:

- `error="insufficient_scope"` — the specific authorization failure.
- `scope="..."` — a space-delimited list of the scopes required for the operation. The server SHOULD include all scopes required for the current operation in a single challenge rather than challenging incrementally.
- `resource_metadata` — the absolute URI of the protected-resource metadata document, for consistency with `401` responses.
- `error_description` — OPTIONAL human-readable description.

```http
HTTP/1.1 403 Forbidden
WWW-Authenticate: Bearer error="insufficient_scope",
                         scope="files:write",
                         resource_metadata="https://mcp.example.com/.well-known/oauth-protected-resource",
                         error_description="File write permission required for this operation"
```

Scope accumulation across operations is a client-side responsibility. When re-authorizing in response to a scope challenge, the client SHOULD request the union of its already-requested scopes and the newly challenged scopes, so already-granted permissions are not lost [SEP-2350]. The client SHOULD then retry the original request with the new token no more than a small, fixed number of times, treating continued failure as a permanent authorization failure, and SHOULD track scope-upgrade attempts to avoid repeated failures for the same resource and operation.

HTTP status codes for authorization errors:

| Status | Meaning | Usage |
| --- | --- | --- |
| `401` | Unauthorized | Authorization required, or token missing/invalid/expired |
| `403` | Forbidden | Invalid scope or insufficient permissions |
| `400` | Bad Request | Malformed authorization request |

### §23.2 Protected Resource Metadata Discovery

The MCP server MUST publish OAuth 2.0 Protected Resource Metadata, and the client MUST use it to discover the server's authorization servers [RFC9728]. The metadata is a JSON document made available through one or both of the following mechanisms; the client MUST support both:

1. **`WWW-Authenticate` header.** The `resource_metadata` parameter of the `WWW-Authenticate` header on a `401` response gives the absolute URI of the metadata document. When present, the client MUST use this URI [RFC9728].
2. **Well-known URI.** The metadata is served at a `.well-known` location. When no `resource_metadata` is available from a header, the client MUST construct and request the well-known URIs in the order below and use the first that returns a valid document [RFC9728][SEP-2351].

Well-known construction, given an MCP server endpoint such as `https://example.com/public/mcp`, MUST be attempted in this order:

1. Path-aware: insert the path component after the well-known suffix —
   `https://example.com/.well-known/oauth-protected-resource/public/mcp`
2. Root: `https://example.com/.well-known/oauth-protected-resource`

The well-known path suffix is `/.well-known/oauth-protected-resource`. If neither location yields a valid document and no header URI was provided, the client MUST abort the authorization attempt or fall back to pre-configured values.

Protected-resource metadata fields the client uses:

```ts
interface ProtectedResourceMetadata {
  // REQUIRED. The protected resource's canonical resource identifier.
  // MUST equal the MCP server's canonical resource identifier.
  resource: string;

  // REQUIRED for MCP. One or more authorization server issuer identifier
  // URLs. MUST contain at least one entry. Each is an independent
  // authorization server; the client selects which to use.
  authorization_servers: string[];

  // OPTIONAL. Scopes the resource recognizes. Used for scope selection
  // when no scope is given in the WWW-Authenticate challenge.
  scopes_supported?: string[];

  // OPTIONAL. Supported methods of presenting the access token; for MCP
  // this is the bearer header method.
  bearer_methods_supported?: string[];
}
```

The `authorization_servers` field MUST be present and MUST contain at least one entry [RFC9728]. The client validates `resource` against the MCP server it is contacting and selects an authorization server from `authorization_servers`.

### §23.3 Authorization Server Metadata Discovery

Having selected an authorization server issuer URL from `authorization_servers`, the client fetches that authorization server's metadata. The authorization server MUST provide at least one of OAuth 2.0 Authorization Server Metadata or OpenID Connect Discovery, and the client MUST support both discovery mechanisms [RFC8414][OIDC].

The well-known suffixes are:

- OAuth 2.0 Authorization Server Metadata: `/.well-known/oauth-authorization-server`
- OpenID Connect Discovery: `/.well-known/openid-configuration`

The client MUST attempt multiple endpoints to handle issuer URLs with and without path components. Path-vs-root ordering [SEP-2351]:

For an issuer URL **with a path component** (for example `https://auth.example.com/tenant1`), attempt in this exact priority order:

1. OAuth 2.0 Authorization Server Metadata with path insertion:
   `https://auth.example.com/.well-known/oauth-authorization-server/tenant1`
2. OpenID Connect Discovery with path insertion:
   `https://auth.example.com/.well-known/openid-configuration/tenant1`
3. OpenID Connect Discovery with path appending:
   `https://auth.example.com/tenant1/.well-known/openid-configuration`

For an issuer URL **without a path component** (for example `https://auth.example.com`), attempt in this exact priority order:

1. OAuth 2.0 Authorization Server Metadata:
   `https://auth.example.com/.well-known/oauth-authorization-server`
2. OpenID Connect Discovery:
   `https://auth.example.com/.well-known/openid-configuration`

After retrieving a metadata document, the client MUST validate it: the `issuer` value in the document MUST be identical to the issuer identifier used to construct the well-known URL [RFC8414][OIDC]. If they differ, the client MUST NOT use the document. For example, a document fetched using issuer `https://attacker.example` that contains `"issuer": "https://honest.example"` MUST be rejected.

Authorization-server metadata fields the client uses:

```ts
interface AuthorizationServerMetadata {
  // REQUIRED. The authorization server's issuer identifier URL. MUST be
  // identical to the value used to construct the discovery URL.
  issuer: string;

  // REQUIRED. URL of the authorization endpoint (used for the
  // authorization-code request).
  authorization_endpoint: string;

  // REQUIRED. URL of the token endpoint (used for the token request).
  token_endpoint: string;

  // OPTIONAL. URL of the Dynamic Client Registration endpoint, when the
  // authorization server supports that mechanism.
  registration_endpoint?: string;

  // OPTIONAL. Scopes the authorization server recognizes.
  scopes_supported?: string[];

  // OPTIONAL. OAuth response_type values supported; MUST include "code"
  // for the authorization-code flow used here.
  response_types_supported?: string[];

  // OPTIONAL. OAuth grant_type values supported; for this flow includes
  // "authorization_code" and, for refresh, "refresh_token".
  grant_types_supported?: string[];

  // OPTIONAL but RECOMMENDED. PKCE code challenge methods supported;
  // MUST include "S256" to interoperate with this flow.
  code_challenge_methods_supported?: string[];

  // OPTIONAL. Client authentication methods supported at the token
  // endpoint (e.g. "none", "private_key_jwt").
  token_endpoint_auth_methods_supported?: string[];

  // OPTIONAL. "true" when the authorization server includes the iss
  // parameter in authorization responses (see Issuer Identification).
  authorization_response_iss_parameter_supported?: boolean;

  // OPTIONAL. "true" when the authorization server accepts HTTPS-URL
  // client identifiers via Client ID Metadata Documents.
  client_id_metadata_document_supported?: boolean;
}
```

### §23.4 Client Registration

Before initiating the authorization-code flow, the client MUST obtain a `client_id` through one of three mechanisms. A client supporting more than one SHOULD apply this priority order:

1. **Pre-registration.** Use pre-registered client information when the client already has it for that authorization server. Pre-registered credentials are specific to a particular authorization server; if the authorization server indicated by protected-resource metadata does not match the one the credentials were registered with, the client SHOULD surface an error rather than silently using mismatched credentials [SEP-2352].
2. **Client ID Metadata Documents** [CIMD]. Use when the authorization server advertises `client_id_metadata_document_supported: true`.
3. **Dynamic Client Registration** [RFC7591]. Use as a fallback when the authorization server exposes a `registration_endpoint`.
4. Otherwise, prompt the user to enter client information.

#### Client ID Metadata Documents

A client MAY use an HTTPS URL as its `client_id`, where the URL resolves to a JSON document describing the client [CIMD]. This addresses the common case where client and server have no prior relationship. Requirements:

- The client MUST host its metadata document at an HTTPS URL. The `client_id` URL MUST use the `https` scheme and MUST contain a path component, for example `https://app.example.com/oauth/client-metadata.json`.
- The document MUST include at least `client_id`, `client_name`, and `redirect_uris`, and the `client_id` value in the document MUST exactly equal the document URL.
- On encountering a URL-formatted `client_id`, the authorization server SHOULD fetch the document, MUST validate that the fetched document's `client_id` matches the URL exactly, MUST validate the redirect URI presented in the authorization request against `redirect_uris` in the document, and MUST validate that the document is valid JSON containing the required fields. The authorization server SHOULD cache the document respecting HTTP cache headers.

Client IDs based on Client ID Metadata Documents are portable across authorization servers; no re-registration is needed when the authorization server changes.

Example metadata document:

```json
{
  "client_id": "https://app.example.com/oauth/client-metadata.json",
  "client_name": "Example MCP Client",
  "client_uri": "https://app.example.com",
  "logo_uri": "https://app.example.com/logo.png",
  "redirect_uris": [
    "http://127.0.0.1:3000/callback",
    "http://localhost:3000/callback"
  ],
  "grant_types": ["authorization_code"],
  "response_types": ["code"],
  "token_endpoint_auth_method": "none"
}
```

#### Dynamic Client Registration (Deprecated)

Dynamic Client Registration is Deprecated; it is retained for compatibility with authorization servers that do not support Client ID Metadata Documents. When supported, the client POSTs a registration request to the authorization server's `registration_endpoint` and receives client credentials in response [RFC7591].

The registration request body includes fields such as `redirect_uris`, `client_name`, `grant_types`, `response_types`, `token_endpoint_auth_method`, `scope`, and `application_type`. The response includes `client_id` and, for confidential clients, `client_secret`.

The client MUST specify an appropriate `application_type` during registration [SEP-837]:

- **Native applications** (desktop, mobile, CLI tools, and locally-hosted applications accessed via `localhost`) SHOULD use `application_type: "native"`.
- **Web applications** (remote browser-based applications served from a non-local host) SHOULD use `application_type: "web"`.

Omitting `application_type` defaults to `"web"` under OpenID Connect, which can conflict with native-style redirect URIs; non-OIDC authorization servers ignore the parameter [SEP-837]. The client MUST be prepared to handle registration failures arising from redirect-URI constraints, SHOULD surface a meaningful error on rejection, and MAY retry with an adjusted `application_type` or conforming redirect URIs.

A client that persists credentials obtained via Dynamic Client Registration MUST associate them with the issuing authorization server, keyed by that authorization server's `issuer`, and MUST re-register when the authorization server changes [SEP-2352].

### §23.5 The Authorization-Code Flow with PKCE

The client obtains an access token using the OAuth 2.0 authorization-code grant with Proof Key for Code Exchange (PKCE) [RFC6749][RFC7636]. PKCE is REQUIRED for this flow; the client MUST use the `S256` code challenge method.

**Step 1 — Generate PKCE parameters and record state.** The client generates a high-entropy `code_verifier` and derives `code_challenge = BASE64URL(SHA-256(code_verifier))` [RFC7636]. Before redirecting the user agent, the client MUST record, in a per-request record associated with the `code_verifier` (and the `state` value, if used), the `issuer` value from the selected authorization server's validated metadata. This recorded issuer is used for the validation in §23.7 Issuer Identification.

**Step 2 — Build the authorization request.** The client directs the user agent to the authorization server's `authorization_endpoint` with these query parameters [RFC6749][RFC7636][RFC8707]:

- `response_type` — MUST be `code`.
- `client_id` — the client identifier obtained during registration.
- `redirect_uri` — the client's redirection URI; MUST match one registered for the client.
- `scope` — the scopes to request. The client SHOULD apply this priority: (1) use the `scope` from the `WWW-Authenticate` challenge if present; (2) otherwise use all scopes in `scopes_supported` from protected-resource metadata, omitting `scope` if `scopes_supported` is absent.
- `state` — an opaque, unguessable value binding the request to the user-agent session; the client SHOULD include it and MUST verify it on the redirect.
- `code_challenge` — the PKCE challenge from Step 1.
- `code_challenge_method` — MUST be `S256`.
- `resource` — the MCP server's canonical resource identifier (see §23.6 Resource Indicators and Audience Binding); MUST be included [RFC8707].

**Step 3 — User authorization and redirect.** The authorization server authenticates the user, obtains consent, and redirects the user agent back to `redirect_uri` with an authorization `code` parameter, the `state` value (if sent), and SHOULD include an `iss` parameter identifying the authorization server [RFC9207]. The client MUST verify `state` matches the value it sent, and MUST validate `iss` per §23.7 Issuer Identification before redeeming the code.

**Step 4 — Token request.** The client exchanges the code at the authorization server's `token_endpoint` with an `application/x-www-form-urlencoded` body [RFC6749][RFC7636][RFC8707]:

- `grant_type` — MUST be `authorization_code`.
- `code` — the authorization code from Step 3.
- `redirect_uri` — MUST be identical to the `redirect_uri` sent in Step 2.
- `code_verifier` — the PKCE verifier from Step 1.
- `client_id` — the client identifier.
- `resource` — the MCP server's canonical resource identifier; MUST be included and MUST be identical to the value sent in Step 2 [RFC8707].

The authorization server returns an access token (and, at its discretion, a refresh token).

### §23.6 Resource Indicators and Audience Binding

The client MUST implement Resource Indicators for OAuth 2.0 [RFC8707]. The `resource` parameter:

1. MUST be included in BOTH the authorization request (Step 2) and the token request (Step 4).
2. MUST identify the MCP server the client intends to use the token with.
3. MUST be the canonical resource identifier of that MCP server (see §23.1 Authorization Model and Applicability).

The client MUST send the `resource` parameter regardless of whether the authorization server advertises support for it. The effect is that the issued access token is audience-bound to that specific MCP server.

The MCP server MUST validate that a presented token was issued specifically for it as the intended audience, and MUST reject tokens not intended for it [RFC8707][SEP-2575]. The MCP server MUST only accept tokens valid for its own resources and MUST NOT accept or forward any other tokens. The client MUST NOT send the MCP server any token other than one issued by that server's authorization server for that server.

Example: to access an MCP server at `https://mcp.example.com`, the authorization and token requests include the URL-encoded resource:

```
&resource=https%3A%2F%2Fmcp.example.com
```

### §23.7 Issuer Identification

To defend against authorization-server mix-up attacks, the client MUST validate the `iss` (issuer) parameter of the authorization response against the issuer it recorded for the selected authorization server (Step 1 in §23.5 The Authorization-Code Flow with PKCE), before transmitting the authorization code to any token endpoint [RFC9207][SEP-2468].

The authorization server SHOULD include the `iss` parameter in authorization responses, including error responses. An authorization server that includes `iss` MUST advertise this by setting `authorization_response_iss_parameter_supported` to `true` in its metadata [RFC9207].

On receiving the authorization response, the client MUST apply the following table, where the recorded issuer is the value stored in Step 1 [RFC9207][SEP-2468]:

| `authorization_response_iss_parameter_supported` | `iss` in response | Client action |
| --- | --- | --- |
| `true` | present | Compare `iss` to the recorded issuer by simple string comparison [RFC3986]. |
| `true` | absent | Reject the response. |
| `false` or absent | present | Compare `iss` to the recorded issuer by simple string comparison [RFC3986]. |
| `false` or absent | absent | Proceed. |

The third row applies the local-policy provision of [RFC9207] Section 2.4: a client MUST compare a present `iss` value against the recorded issuer regardless of whether the authorization server advertises `authorization_response_iss_parameter_supported`, so that an authorization server which emits `iss` without having advertised it in its metadata is still validated [RFC9207].

The comparison is an exact string match. After decoding the `iss` value from the `application/x-www-form-urlencoded` response, the client MUST NOT apply scheme or host case folding, default-port elision, trailing-slash, or percent-encoding normalization before comparison [RFC9207][RFC3986]. This validation applies equally to error responses: on mismatch the client MUST NOT act on or display `error`, `error_description`, or `error_uri`.

### §23.8 Access Token Usage

The client presents the access token as a bearer token in the HTTP `Authorization` header on every request to the MCP server [RFC6750]. Consistent with the stateless per-request model in §4 Request Metadata and the Stateless Model, authorization MUST be included in every HTTP request from client to server; the server treats each request independently and revalidates the token each time.

- The client MUST use the HTTP `Authorization` request header field with the `Bearer` scheme: `Authorization: Bearer <access-token>` [RFC6750].
- The access token MUST NOT be placed in the URI query string [RFC6750].

On every request, the MCP server MUST validate the access token: its signature or introspection result, its expiry, its audience (that the token was issued for this server, per §23.6 Resource Indicators and Audience Binding), and its scope against what the operation requires [RFC6750][RFC8707]. If validation fails, the server MUST respond with `401 Unauthorized` for missing/invalid/expired tokens and SHOULD respond with `403 Forbidden` with an `insufficient_scope` challenge for valid tokens that lack required scope (see the Insufficient-Scope Response in §23.1 Authorization Model and Applicability).

### §23.9 Refresh Tokens

A client that desires refresh capability participates in the refresh-token grant [SEP-2207]:

- It SHOULD include `refresh_token` in its `grant_types` client metadata.
- It MAY add `offline_access` to the `scope` parameter of the authorization and token requests when the authorization-server metadata lists `offline_access` in `scopes_supported`.
- It MUST keep refresh tokens confidential in transit and storage.
- It MUST NOT assume a refresh token will be issued; the authorization server retains discretion.

To obtain a new access token, the client makes a token request to the `token_endpoint` with `grant_type=refresh_token`, the `refresh_token` value, and the same `resource` parameter (the MCP server's canonical resource identifier) so the refreshed token remains audience-bound [RFC8707][SEP-2207]. The client MAY include a `scope` parameter to narrow the requested scopes.

The MCP server, as a protected resource, SHOULD NOT include `offline_access` in its `WWW-Authenticate` `scope` parameter or in protected-resource metadata `scopes_supported`, because refresh tokens are not a resource requirement [SEP-2207].

### §23.10 Worked HTTP Examples

The following examples use the revision string `2026-07-28` where a revision is shown, an MCP server at `https://mcp.example.com`, and an authorization server at `https://auth.example.com`.

**Unauthorized response with `WWW-Authenticate`:**

```http
HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer resource_metadata="https://mcp.example.com/.well-known/oauth-protected-resource",
                         scope="files:read"
```

**Protected-resource metadata document:**

```json
{
  "resource": "https://mcp.example.com",
  "authorization_servers": ["https://auth.example.com"],
  "scopes_supported": ["files:read", "files:write"],
  "bearer_methods_supported": ["header"]
}
```

**Authorization request URL (with PKCE and resource):**

```
https://auth.example.com/authorize
  ?response_type=code
  &client_id=https%3A%2F%2Fapp.example.com%2Foauth%2Fclient-metadata.json
  &redirect_uri=http%3A%2F%2Flocalhost%3A3000%2Fcallback
  &scope=files%3Aread
  &state=af0ifjsldkj
  &code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM
  &code_challenge_method=S256
  &resource=https%3A%2F%2Fmcp.example.com
```

**Redirect back to the client (with code and issuer):**

```http
HTTP/1.1 302 Found
Location: http://localhost:3000/callback?code=SplxlOBeZQQYbYS6WxSbIA&state=af0ifjsldkj&iss=https%3A%2F%2Fauth.example.com
```

The client compares the decoded `iss` value `https://auth.example.com` to the recorded issuer by exact string match before redeeming the code.

**Token request:**

```http
POST /token HTTP/1.1
Host: auth.example.com
Content-Type: application/x-www-form-urlencoded

grant_type=authorization_code&code=SplxlOBeZQQYbYS6WxSbIA&redirect_uri=http%3A%2F%2Flocalhost%3A3000%2Fcallback&code_verifier=dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk&client_id=https%3A%2F%2Fapp.example.com%2Foauth%2Fclient-metadata.json&resource=https%3A%2F%2Fmcp.example.com
```

**Token response:**

```http
HTTP/1.1 200 OK
Content-Type: application/json
Cache-Control: no-store

{
  "access_token": "eyJhbGciOiJIUzI1NiIs...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "tGzv3JOkF0XG5Qx2TlKWIA",
  "scope": "files:read"
}
```

**Authorized MCP request carrying the bearer token:**

```http
POST /mcp HTTP/1.1
Host: mcp.example.com
Content-Type: application/json
Accept: application/json, text/event-stream
MCP-Protocol-Version: 2026-07-28
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...

{"jsonrpc":"2.0","id":1,"method":"tools/list"}
```

**Refresh-token request:**

```http
POST /token HTTP/1.1
Host: auth.example.com
Content-Type: application/x-www-form-urlencoded

grant_type=refresh_token&refresh_token=tGzv3JOkF0XG5Qx2TlKWIA&client_id=https%3A%2F%2Fapp.example.com%2Foauth%2Fclient-metadata.json&resource=https%3A%2F%2Fmcp.example.com
```

### §23.11 Client Registration Mechanisms

Before initiating an authorization flow, a client MUST obtain a `client_id` for use with the authorization server discovered for the target MCP server (authorization-server discovery and the authorization-code flow are defined in §23.3 Authorization Server Metadata Discovery and §23.5 The Authorization-Code Flow with PKCE; the HTTP transport that carries the `Authorization` header and the `401`/`403` challenges is defined in §9 The Streamable HTTP Transport). Three registration mechanisms are defined: **Client ID Metadata Documents** (the client is identified by an HTTPS URL), **pre-registration** (the client already holds a `client_id`, and possibly a secret, for that authorization server), and **Dynamic Client Registration** (the client obtains a `client_id` programmatically; Deprecated). [MCP][CIMD][RFC7591]

#### Mechanism Selection Priority

A client that supports more than one mechanism SHOULD attempt them in the following order, using the first that applies: [MCP]

1. Use pre-registered client information for the target authorization server if the client already has it available.
2. Use a Client ID Metadata Document if the authorization server indicates support for it (its authorization-server metadata contains `"client_id_metadata_document_supported": true`). [CIMD]
3. Use Dynamic Client Registration as a fallback if the authorization server advertises a `registration_endpoint` in its authorization-server metadata. [RFC7591]
4. If none of the above is available, prompt the user to supply the client information.

A client MUST check the authorization-server metadata before choosing a mechanism: it MUST NOT attempt Client ID Metadata Documents unless `client_id_metadata_document_supported` is `true`, and MUST NOT attempt Dynamic Client Registration unless `registration_endpoint` is present.

### §23.12 Client ID Metadata Documents

A Client ID Metadata Document (CIMD) registration uses an HTTPS URL as the `client_id`. The URL resolves, via an HTTP `GET`, to a JSON document describing the client. This removes any need for a prior relationship between the client and the authorization server: the authorization server fetches the client's self-hosted metadata on demand. Clients and authorization servers SHOULD support this mechanism. [CIMD]

#### Client Requirements

A client using a Client ID Metadata Document MUST satisfy all of the following: [CIMD]

- The client MUST host its metadata document at an HTTPS URL.
- The `client_id` URL MUST use the `https` scheme and MUST contain a path component. For example, `https://app.example.com/oauth/client-metadata.json`.
- The metadata document MUST be a valid JSON object [RFC8259] containing at least the fields `client_id`, `client_name`, and `redirect_uris`.
- The `client_id` value inside the document MUST equal the document's own URL exactly (byte-for-byte; no normalization is applied).
- The client MAY authenticate to the token endpoint using `private_key_jwt` with an appropriate JWKS configuration; in that case the document conveys the key material per the requirements of [CIMD].

The fields of the metadata document are:

```ts
interface ClientIdMetadataDocument {
  // REQUIRED. The HTTPS URL of this document; MUST equal the URL it is served from.
  client_id: string;
  // REQUIRED. Human-readable client name shown on the consent page.
  client_name: string;
  // REQUIRED. Allowed redirect URIs for the authorization code flow.
  redirect_uris: string[];
  // OPTIONAL. Home page of the client.
  client_uri?: string;
  // OPTIONAL. URL of the client's logo.
  logo_uri?: string;
  // OPTIONAL. OAuth grant types the client uses (e.g., "authorization_code").
  grant_types?: string[];
  // OPTIONAL. OAuth response types the client uses (e.g., "code").
  response_types?: string[];
  // OPTIONAL. Token-endpoint authentication method (e.g., "none", "private_key_jwt").
  token_endpoint_auth_method?: string;
  // OPTIONAL. Additional OAuth client metadata fields MAY appear.
  [key: string]: unknown;
}
```

#### Authorization-Server Consumption

When an authorization server receives an authorization request whose `client_id` is a URL, it consumes the document as follows: [CIMD]

- It SHOULD fetch the metadata document by performing an HTTP `GET` on the `client_id` URL.
- It MUST validate that the fetched document's `client_id` matches the URL it was fetched from, exactly.
- It MUST validate that the document is valid JSON and contains the required fields (`client_id`, `client_name`, `redirect_uris`).
- It MUST validate the `redirect_uri` presented in the authorization request against the `redirect_uris` in the metadata document, rejecting requests whose redirect URI is not listed.
- It SHOULD cache the metadata document, respecting HTTP cache headers.
- It SHOULD apply the security considerations of [CIMD] (for example, a trust policy over the allowed client-hosting domains). The broader treatment of these threats is in §28 Security Considerations.

On validation failure the authorization server returns an OAuth error such as `error=invalid_client` or `error=invalid_request`.

A `client_id` based on a Client ID Metadata Document is portable across authorization servers, because it is a self-hosted HTTPS URL resolved on demand. No re-registration is required when the target authorization server changes.

#### Advertising Support

An authorization server advertises support for Client ID Metadata Documents by setting the following field in its authorization-server metadata: [CIMD]

```json
{
  "client_id_metadata_document_supported": true
}
```

#### Example Metadata Document

```json
{
  "client_id": "https://app.example.com/oauth/client-metadata.json",
  "client_name": "Example MCP Client",
  "client_uri": "https://app.example.com",
  "logo_uri": "https://app.example.com/logo.png",
  "redirect_uris": [
    "http://127.0.0.1:3000/callback",
    "http://localhost:3000/callback"
  ],
  "grant_types": ["authorization_code"],
  "response_types": ["code"],
  "token_endpoint_auth_method": "none"
}
```

### §23.13 Pre-Registration

A client SHOULD support static client credentials supplied out of band. These may be hardcoded for use with a specific authorization server, or entered by the user through a configuration interface after the user has registered an OAuth client with the authorization server. Pre-registered credentials are specific to the authorization server that issued them; see §23.16 Credential Binding to the Issuer. [MCP]

### §23.14 Dynamic Client Registration

**Status: Deprecated.** Dynamic Client Registration remains available for authorization servers that do not support Client ID Metadata Documents; new implementations SHOULD use Client ID Metadata Documents instead. Clients and authorization servers MAY support Dynamic Client Registration to let a client obtain a `client_id` programmatically, without user interaction. [RFC7591]

A client performs Dynamic Client Registration by sending an HTTP `POST` of a JSON client-metadata object to the `registration_endpoint` published in the authorization-server metadata. The authorization server responds with the issued client information. [RFC7591]

The request body is a JSON object of client metadata. At minimum it conveys the redirect URIs and the application type (see §23.15 Application Type):

```ts
interface ClientRegistrationRequest {
  // REQUIRED. Redirect URIs for the authorization code flow.
  redirect_uris: string[];
  // REQUIRED for MCP clients. "native" or "web" (see Application Type).
  application_type: string;
  // OPTIONAL. Human-readable client name.
  client_name?: string;
  // OPTIONAL. Grant types the client will use (e.g., ["authorization_code", "refresh_token"]).
  grant_types?: string[];
  // OPTIONAL. Response types the client will use (e.g., ["code"]).
  response_types?: string[];
  // OPTIONAL. Token-endpoint authentication method (e.g., "none").
  token_endpoint_auth_method?: string;
  // OPTIONAL. Space-delimited scopes the client may request.
  scope?: string;
  // OPTIONAL. Additional OAuth client metadata fields MAY appear.
  [key: string]: unknown;
}
```

The response body returns the issued `client_id` (REQUIRED) together with any registered metadata. If the authorization server issues a secret it returns `client_secret` and its expiry:

```ts
interface ClientRegistrationResponse {
  // REQUIRED. The issued client identifier.
  client_id: string;
  // OPTIONAL. Issued client secret for confidential clients.
  client_secret?: string;
  // OPTIONAL. Time the client_id was issued, seconds since the Unix epoch.
  client_id_issued_at?: number;
  // OPTIONAL. Expiry of client_secret, seconds since the Unix epoch; 0 means no expiry.
  client_secret_expires_at?: number;
  // The registered metadata is echoed back (e.g., redirect_uris, application_type, ...).
  [key: string]: unknown;
}
```

#### Example Dynamic Client Registration

```http
POST /register HTTP/1.1
Host: auth.example.com
Content-Type: application/json

{
  "client_name": "Example MCP Client",
  "application_type": "native",
  "redirect_uris": [
    "http://127.0.0.1:3000/callback",
    "http://localhost:3000/callback"
  ],
  "grant_types": ["authorization_code", "refresh_token"],
  "response_types": ["code"],
  "token_endpoint_auth_method": "none"
}
```

```http
HTTP/1.1 201 Created
Content-Type: application/json

{
  "client_id": "s6BhdRkqt3",
  "client_id_issued_at": 1769555200,
  "application_type": "native",
  "redirect_uris": [
    "http://127.0.0.1:3000/callback",
    "http://localhost:3000/callback"
  ],
  "grant_types": ["authorization_code", "refresh_token"],
  "response_types": ["code"],
  "token_endpoint_auth_method": "none"
}
```

### §23.15 Application Type

When performing Dynamic Client Registration, a client MUST specify an `application_type` that is consistent with its redirect URIs, because authorization servers that implement OpenID Connect may enforce additional redirect-URI constraints based on this value. Omitting `application_type` defaults to `"web"` under OIDC, which can conflict with native-style (loopback) redirect URIs; non-OIDC servers ignore the parameter safely. [OIDC][SEP-837]

The value is chosen from the nature of the client and its redirect URIs:

- A **native application** — a desktop application, a mobile app, a CLI tool, or a locally hosted web application reached via `localhost` or a loopback IP address — SHOULD use `application_type: "native"`.
- A **web application** — a remote browser-based application served from a non-local host — SHOULD use `application_type: "web"`.

A client MUST be prepared for registration to fail because of redirect-URI constraints when the authorization server implements OIDC. On rejection, the client SHOULD surface a meaningful error to the user or developer, and MAY retry registration with an adjusted `application_type` or with redirect URIs that conform to the authorization server's requirements for the given application type. [OIDC][SEP-837]

### §23.16 Credential Binding to the Issuer

A client that uses pre-registered credentials, or that persists credentials obtained via Dynamic Client Registration (the `client_id`, any `client_secret`, and the registration), MUST associate those credentials with the specific authorization server that issued them, keyed by that authorization server's `issuer` identifier as it appears in the validated authorization-server metadata. [SEP-2352]

The following requirements apply: [SEP-2352]

- The storage key for persisted client credentials MUST be the authorization server's `issuer` identifier.
- A client MUST NOT reuse credentials issued by one authorization server with a different authorization server. Client identifiers are unique to the authorization server that issued them, so each authorization server requires separate registration state. [RFC6749]
- When the target MCP server's protected-resource metadata indicates a different authorization server than the one the credentials were registered with (detected by comparing the `issuer`), the client MUST NOT reuse the existing credentials and MUST re-register with the new authorization server.
- The comparison between the stored issuer and the discovered issuer MUST be an exact string match (simple string comparison of the issuer identifiers); no scheme/host case folding, default-port elision, trailing-slash, or percent-encoding normalization is applied. [SEP-2352][SEP-2468]
- If the authorization server indicated by protected-resource metadata does not match the one the pre-registered credentials were registered with, the client SHOULD surface an error rather than silently attempting to use mismatched credentials.

Credentials based on a Client ID Metadata Document are exempt from re-registration, because the `client_id` is a portable self-hosted HTTPS URL that the authorization server resolves on demand; no per-issuer registration state exists to bind. [CIMD]

### §23.17 Discovery Robustness

A client locates two kinds of metadata: the MCP server's protected-resource metadata, and the authorization server's metadata. The full discovery flow is defined in §23.2 Protected Resource Metadata Discovery and §23.3 Authorization Server Metadata Discovery; the path-construction rules below ensure a client can locate metadata for issuers both with and without path components. [SEP-2351]

#### Protected-Resource Metadata

The protected-resource metadata well-known path suffix is `/.well-known/oauth-protected-resource`. A client locates the document as follows: [RFC9728][SEP-2351]

- If the MCP server returned a `WWW-Authenticate` header with a `resource_metadata` parameter on a `401 Unauthorized` response, the client MUST use that URL.
- Otherwise the client MUST fall back to constructing and requesting the well-known URIs, in this order:
  1. The suffix prefixed to the MCP endpoint's path component. For an MCP endpoint `https://example.com/public/mcp`, this is `https://example.com/.well-known/oauth-protected-resource/public/mcp`.
  2. The suffix at the host root: `https://example.com/.well-known/oauth-protected-resource`.

The protected-resource metadata document MUST contain the `authorization_servers` field, an array of one or more authorization-server issuer identifiers. When more than one is listed, each is an independent authorization server and the client is responsible for selecting which to use; the client MUST maintain separate registration state per authorization server. [RFC9728]

#### Authorization-Server Metadata

MCP uses the default `oauth-authorization-server` well-known URI suffix; it defines no application-specific suffix. A client MUST attempt multiple well-known endpoints, derived from the authorization server's `issuer` identifier, to interoperate with both OAuth 2.0 Authorization Server Metadata and OpenID Connect Discovery 1.0. [RFC8414][SEP-2351]

For an issuer URL **with** a path component (for example `https://auth.example.com/tenant1`), the client MUST try, in this priority order:

1. OAuth 2.0 Authorization Server Metadata with path insertion: `https://auth.example.com/.well-known/oauth-authorization-server/tenant1`
2. OpenID Connect Discovery with path insertion: `https://auth.example.com/.well-known/openid-configuration/tenant1`
3. OpenID Connect Discovery with path appending: `https://auth.example.com/tenant1/.well-known/openid-configuration`

For an issuer URL **without** a path component (for example `https://auth.example.com`), the client MUST try, in this priority order:

1. OAuth 2.0 Authorization Server Metadata: `https://auth.example.com/.well-known/oauth-authorization-server`
2. OpenID Connect Discovery: `https://auth.example.com/.well-known/openid-configuration`

After retrieving an authorization-server metadata document, the client MUST validate that the `issuer` value in the document is identical to the issuer identifier used to construct the well-known URL; if they differ, the client MUST NOT use the metadata. For example, a document fetched while expecting issuer `https://honest.example` that instead contains `"issuer": "https://attacker.example"` MUST be rejected. [RFC8414][SEP-2468]

### §23.18 Scopes and Step-Up Authorization

During the initial authorization handshake, a client SHOULD follow the principle of least privilege and select scopes in this priority order: [SEP-2350]

1. Use the `scope` parameter from the initial `WWW-Authenticate` header on the `401 Unauthorized` response, if present. A client MUST treat the challenged scopes as authoritative for the current operation and MUST NOT assume any set relationship between them and `scopes_supported`.
2. If no `scope` parameter is present, use all scopes from `scopes_supported` in the protected-resource metadata, omitting the `scope` parameter entirely if `scopes_supported` is absent.

#### Insufficient-Scope Challenge

When a client makes a request with a token that lacks a required scope, the server SHOULD respond with: [RFC6750][SEP-2350]

- HTTP status `403 Forbidden`.
- A `WWW-Authenticate` header using the `Bearer` scheme with these parameters:
  - `error="insufficient_scope"` — REQUIRED; identifies the authorization failure.
  - `scope="<space-delimited scopes>"` — RECOMMENDED; the scopes required to satisfy the current operation.
  - `resource_metadata="<URL>"` — RECOMMENDED; the URL of the protected-resource metadata document, for consistency with `401` challenges.
  - `error_description="<text>"` — OPTIONAL; a human-readable description.

The `scope` attribute describes the scopes necessary for the requested operation. A server is not required to include the client's already-granted scopes, and SHOULD include all scopes required for the current operation in a single challenge rather than challenging for one missing scope at a time. A server SHOULD be consistent in its scope-inclusion strategy.

The relationship between HTTP status and authorization condition is:

| Status | Meaning | Usage |
| --- | --- | --- |
| `401` | Unauthorized | Authorization required, or token absent/invalid |
| `403` | Forbidden | Invalid scopes or insufficient permissions |
| `400` | Bad Request | Malformed authorization request |

(This is the authorization-error status mapping; the general HTTP status mapping for the transport is in §9 The Streamable HTTP Transport. The JSON-RPC error codes used in MCP message payloads, including `INVALID_PARAMS` (`-32602`) and the others, are defined in §22 Error Handling and Error Codes; the result-type discriminator is defined in §3 Base Message Format.)

#### Step-Up Authorization Flow

On receiving a scope-related error (during initial authorization or an `insufficient_scope` error at runtime), a client SHOULD obtain a new access token with an increased scope set via a step-up authorization flow. A client acting on behalf of a user SHOULD attempt the step-up flow; a client acting on its own behalf (a `client_credentials` client) MAY attempt the flow or abort the request. The flow is: [SEP-2350]

1. **Parse the error** from the authorization-server response or the `WWW-Authenticate` header.
2. **Determine the required scopes** by computing the **union** of the client's already-requested scope set and the scopes from the current challenge. The client MUST accumulate (union) already-granted scopes with the newly challenged scopes; it MUST NOT drop the already-granted scopes, or it would lose permissions needed for other operations. Scope accumulation across operations is a client-side responsibility, which lets servers remain stateless with respect to client scope sets.
3. **Initiate re-authorization** with the unioned scope set (a fresh authorization-code flow with PKCE, the `resource` parameter, and recorded expected `issuer`, as defined in §23.5 The Authorization-Code Flow with PKCE).
4. **Retry the original request** with the new token, no more than a few times; persistent failure MUST be treated as a permanent authorization failure.

A client SHOULD implement retry limits and SHOULD track scope-upgrade attempts to avoid repeated failures for the same resource-and-operation combination. A client need not deduplicate hierarchically redundant scopes (for example a broad `admin` scope that already implies a challenged `read`); authorization servers normalize such redundancy during token issuance.

#### Example: 403 Insufficient-Scope Challenge and Step-Up

A runtime request whose token is missing the `files:write` scope:

```http
HTTP/1.1 403 Forbidden
WWW-Authenticate: Bearer error="insufficient_scope",
                         scope="files:write",
                         resource_metadata="https://mcp.example.com/.well-known/oauth-protected-resource",
                         error_description="File write permission required for this operation"
```

The client already held `files:read`. It unions the prior scope with the challenged `files:write` and initiates a step-up authorization request that preserves both:

```http
GET /authorize?response_type=code&client_id=https%3A%2F%2Fapp.example.com%2Foauth%2Fclient-metadata.json&redirect_uri=http%3A%2F%2F127.0.0.1%3A3000%2Fcallback&code_challenge=K2-ltc83acc4h0c9w6ESC_rEMTJ3bww-uCHaoeK1t8U&code_challenge_method=S256&state=af0ifjsldkj&scope=files%3Aread%20files%3Awrite&resource=https%3A%2F%2Fmcp.example.com%2Fmcp HTTP/1.1
Host: auth.example.com
```

The `scope` parameter carries the union `files:read files:write` (URL-encoded as `files%3Aread%20files%3Awrite`); dropping `files:read` would lose the already-granted permission.

### §23.19 Authorization Security Considerations

The following requirements harden the authorization flow. The broader treatment of these and related threats is in §28 Security Considerations.

- **Audience-bound tokens (no token passthrough; confused-deputy defense).** A client MUST implement Resource Indicators by sending a `resource` parameter, identifying the MCP server by its canonical URI, in both the authorization request and the token request, regardless of whether the authorization server is known to support it. A server MUST validate that an access token was issued specifically for it as the intended audience, and MUST reject tokens whose audience does not match. A client MUST NOT send a token to an MCP server other than one issued by that MCP server's authorization server, and a server MUST NOT accept or transit any other token. This prevents a server from being used as a confused deputy that replays a token to a third party. [RFC8707][RFC6750]
- **Exact issuer validation (mix-up defense).** A client MUST record the validated `issuer` of the selected authorization server before redirecting the user-agent, and MUST validate the `iss` value returned in the authorization response against the recorded issuer using exact string comparison before transmitting the authorization code to any token endpoint. If the authorization server's metadata sets `authorization_response_iss_parameter_supported` to `true` and `iss` is absent from the response, the client MUST reject the response; if `iss` is present, the client MUST compare it exactly regardless of metadata. On mismatch the client MUST NOT act on or display the authorization code or any `error`, `error_description`, or `error_uri`. The recorded issuer, PKCE code verifier, and `state` MUST be stored in the same per-request record. [RFC9207][SEP-2468]
- **PKCE (authorization-code interception defense).** A client MUST use Proof Key for Code Exchange: it MUST generate a `code_verifier`, send the derived `code_challenge` with `code_challenge_method=S256` in the authorization request, and send the `code_verifier` in the token request. [RFC7636]
- **`state` parameter (CSRF defense).** A client SHOULD include an unpredictable `state` value in the authorization request and verify it on the callback, binding the response to the request and defending against cross-site request forgery.
- **Token confidentiality.** Access tokens and refresh tokens MUST NOT be logged, and MUST NOT be forwarded to third parties; refresh tokens MUST be kept confidential in transit and storage. A client MUST send the access token only in the `Authorization: Bearer <access-token>` request header on every request to the MCP server, and MUST NOT place the token in the URI query string. [RFC6750]

#### Refresh Tokens

A client that wants refresh tokens MUST keep them confidential in transit and storage, SHOULD include `refresh_token` in its `grant_types` client metadata, and MAY add `offline_access` to the `scope` parameter of the authorization and token requests when the authorization-server metadata lists `offline_access` in `scopes_supported`. A client MUST NOT assume a refresh token will be issued; the authorization server retains discretion. A server (protected resource) SHOULD NOT include `offline_access` in its `WWW-Authenticate` scope or in `scopes_supported`, because refresh tokens are not a resource requirement. [SEP-2207]

# Part VII — Extensions

## §24 The Extension Mechanism

This section defines the framework by which functionality beyond the core protocol is added, negotiated, and used. The two extensions defined by this document — §25 The Tasks Extension and §26 The Interactive User-Interface Extension — are built on this framework, and any third-party extension MUST conform to it [SEP-2133].

### §24.1 Definition and Scope

An **extension** is an optional, self-contained unit of additional protocol functionality that is negotiated between peers and is opt-in [SEP-2133]. An extension MAY add features that are modular (a discrete capability), specialized (domain- or industry-specific behavior), or experimental (functionality being incubated for possible future inclusion in the core protocol) [MCP].

The following rules govern the relationship between extensions and the core protocol:

- Core conformance does NOT require support for any extension. An implementation that supports zero extensions is fully conformant with the core protocol [SEP-2133].
- An implementation MAY support any number of extensions.
- Extensions are disabled by default. An implementation MUST NOT treat an extension as active unless it has been negotiated as specified in §24.3 [SEP-2133].
- An extension is versioned and evolves independently of the core protocol revision (see §24.6 and §5 Protocol Revision, Version Negotiation, and Discovery).

An extension is the only sanctioned mechanism for adding request methods, notifications, result-type discriminator values, reserved metadata keys, or fields that are not part of the core protocol. Adding such surface outside the extension mechanism is non-conformant.

### §24.2 Extension Identifiers

Every extension is named by a single, globally unique **extension identifier**. The identifier is the key under which the extension is advertised in the negotiation map (§24.3) and is the namespace root from which the extension's methods, notifications, and reserved metadata keys are derived (§24.5).

An extension identifier has the form:

```
extension-identifier = vendor-prefix "/" extension-name
```

where the `vendor-prefix` is a reverse-DNS-style prefix and `extension-name` is a single name segment. Extension identifiers follow the same lexical rules as reserved metadata (`_meta`) key names defined in §4 Request Metadata and the Stateless Model, with the prefix being mandatory (a bare name with no prefix is NOT a valid extension identifier).

**Vendor prefix grammar.** The `vendor-prefix` is a series of one or more labels separated by dots (`.`), terminated by the `/` separator:

```
vendor-prefix = label *( "." label ) "/"
label         = ALPHA *( ALPHA / DIGIT / "-" ) ( ALPHA / DIGIT )
```

That is, each label MUST start with a letter and end with a letter or digit; interior characters MAY be letters, digits, or hyphens (`-`). Implementations SHOULD use reverse DNS notation derived from a domain the vendor controls (for example, the owner of `example.com` uses the prefix `com.example/`), to avoid collisions [MCP].

**Extension name grammar.** The `extension-name` follows the `_meta` name rules of §4:

```
extension-name = alphanumeric *( alphanumeric / "-" / "_" / "." ) alphanumeric
               / alphanumeric
alphanumeric   = ALPHA / DIGIT
```

That is, the name MUST begin and end with an alphanumeric character (`[a-z0-9A-Z]`) and MAY contain hyphens (`-`), underscores (`_`), dots (`.`), and alphanumerics in between.

**Reserved prefixes.** Any vendor prefix whose second label is `modelcontextprotocol` or `mcp` is RESERVED to the core protocol and to extensions published as part of it; third-party extensions MUST NOT use such a prefix [MCP]. The reservation matches on the second label specifically:

- RESERVED (MUST NOT be used by third parties): `io.modelcontextprotocol/`, `org.modelcontextprotocol.api/`, `dev.mcp/`, `com.mcp.tools/`.
- NOT reserved (a third party MAY use it): `com.example.mcp/` — the second label is `example`, not `modelcontextprotocol` or `mcp`.

The bare tokens `modelcontextprotocol` and `mcp` MUST NOT be used as vendor prefixes by third-party extensions.

**Case-sensitivity.** Extension identifiers are case-sensitive. `Com.Example/Ext` and `com.example/ext` are distinct identifiers, and a peer matching an advertised identifier MUST compare it octet-for-octet without case folding.

Examples of well-formed identifiers:

```
io.modelcontextprotocol/tasks
io.modelcontextprotocol/ui
io.modelcontextprotocol/oauth-client-credentials
com.example/my-extension
```

### §24.3 Negotiation

Extensions are advertised through the `extensions` map carried in `ClientCapabilities` and `ServerCapabilities`. The shape of the capabilities object and the per-request capability model that delivers it are defined in §6 Capabilities and Extensions and §4 Request Metadata and the Stateless Model; this subsection specifies only the semantics of the `extensions` map.

The `extensions` map is keyed by extension identifier (§24.2), and each value is a **settings object** for that extension:

```ts
interface ClientCapabilities {
  // ... other capability fields, see §6 ...
  extensions?: { [extensionIdentifier: string]: ExtensionSettings };
}

interface ServerCapabilities {
  // ... other capability fields, see §6 ...
  extensions?: { [extensionIdentifier: string]: ExtensionSettings };
}

type ExtensionSettings = { [key: string]: unknown };
```

Field rules:

- `extensions` — OPTIONAL. An object whose keys are extension identifiers the peer supports and offers. Absence of the field, or an empty object, means the peer advertises no extensions.
- Each map **value** is a settings object (a JSON object) whose contents are defined by the named extension. An **empty object** (`{}`) means the extension is enabled with no settings. A map value MUST NOT be `null`. A peer that receives a `null` value for an extension key MUST treat that entry as malformed and MUST NOT activate the extension.

**Active set.** An extension is **ACTIVE** for an interaction only when BOTH peers advertise its identifier — that is, the active set is the intersection of the identifiers in the client's `extensions` map and the identifiers in the server's `extensions` map. Each peer determines the active set independently by computing this intersection from the advertised maps (the server obtains the client's capabilities from `io.modelcontextprotocol/clientCapabilities` in request metadata per §4; the client obtains the server's capabilities from the `server/discover` result per §6).

A peer MUST NOT use, invoke, or rely on an extension the other side did not advertise. Specifically, a peer MUST NOT send a method, notification, reserved metadata key, result-type discriminator value, or additional field that is defined by an extension not present in the active set. Sending such surface for a non-active extension is non-conformant; a receiver encountering it MAY reject the message with a core error (see §22 Error Handling and Error Codes) or ignore the unknown surface per the forward-compatibility rules of §2 Conformance, Terminology, and Notation.

When an extension settings object carries configuration (for example a version or a list of supported MIME types), each peer SHOULD reconcile its own advertised settings with the other peer's advertised settings to determine the precise behavior to use, according to that extension's own rules.

### §24.4 Per-Request Application of the Active Set

Because the protocol is stateless and capabilities are supplied per request (§4 Request Metadata and the Stateless Model), the active set is determined from the capabilities presented for each request rather than from any prior connection state. A server MUST recompute the intersection from the `io.modelcontextprotocol/clientCapabilities` of the request it is processing and MUST NOT infer an extension's activation from a previous request on the same connection or stream. A client that does not advertise an extension on a given request MUST be served as if that extension were inactive for that request.

### §24.5 Extending the Protocol Surface

An active extension MAY extend the protocol surface in the following ways, and ONLY in these ways:

1. **Additional request methods and notifications.** An extension MAY define new JSON-RPC methods and notifications. Their `method` strings MUST be namespaced so as not to collide with core methods or with other extensions; the extension's own identifier-derived namespace is the basis for this naming (for example, the Tasks extension of §25 defines methods such as `tasks/get`, `tasks/update`, and `tasks/cancel`). A method or notification defined by an extension MUST NOT be sent unless the extension is in the active set (§24.3).

2. **Additional reserved `_meta` keys.** An extension MAY reserve additional metadata keys carried in `_meta`. Such keys MUST be named under a vendor prefix that the extension controls, following the prefix rules of §4 Request Metadata and the Stateless Model. Keys defined by core-protocol extensions use a reserved prefix (for example `io.modelcontextprotocol/`); third-party extensions use their own vendor prefix.

3. **Additional `resultType` discriminator values.** The `resultType` discriminator defined in §3 Base Message Format is an open set. An extension MAY define additional `resultType` values. The set of `resultType` values a receiver will accept MUST be formed from the core-protocol values together with the values contributed by extensions in the active set; a `resultType` value that is neither a core value nor contributed by an active extension MUST be treated as invalid by the receiver (§3 Base Message Format, §22 Error Handling and Error Codes).

4. **Additional fields on existing objects.** An extension MAY add fields to existing core objects. A peer that does not support the extension (or for which the extension is not active) MUST ignore such unknown fields, in accordance with the forward-compatibility rules of §2 Conformance, Terminology, and Notation. Conversely, a peer MUST NOT depend on an extension-defined field being honored unless the extension is in the active set.

An extension MUST NOT redefine the meaning of an existing core method, notification, field, or `resultType` value; it may only add new surface.

### §24.6 Versioning, Stability, and Deprecation

An extension is versioned independently of the core protocol revision. The core revision string (for example `"2026-07-28"`) governs the core protocol only and does not version any extension.

How an extension expresses its own version is defined by that extension. An extension that requires version discrimination SHOULD express its version within its settings object (for example a `version` field) or via capability flags in that object, so that the version is discoverable through negotiation (§24.3). Whatever the mechanism, an extension's version, where it has one, MUST be discoverable through the negotiation map; a peer MUST NOT be required to infer an extension's version from out-of-band information.

For backward-compatible evolution within a single identifier, an extension SHOULD prefer adding capability flags or version markers inside its settings object over minting a new identifier. Where an incompatible change cannot be avoided, the changed extension SHOULD be published under a new extension identifier so that the two remain distinct in the negotiation map (for example `com.example/my-extension` and `com.example/my-extension-2`); the two identifiers are then independent entries that peers negotiate separately. A change is incompatible when it would cause an existing implementation to fail or behave incorrectly — for example removing or renaming a field, changing a field's type, altering the semantics of existing behavior, or adding a new required field.

An extension, or a feature within an extension, MAY be marked **Deprecated** as a present condition (see §27 Feature Lifecycle and Deprecation). A Deprecated extension remains negotiable and, when active, behaves as specified; the marking signals that its use is discouraged.

### §24.7 Graceful Degradation

When an extension is NOT in the active set, both peers MUST fall back to core protocol behavior for the affected interaction. Concretely:

- A peer MUST NOT emit any surface defined by a non-active extension (§24.5), and MUST instead use the corresponding core behavior. For example, a server offering tools whose output is enriched by the Interactive User-Interface extension (§26) MUST still return meaningful core content for a client with which that extension is not active.
- An implementation that genuinely requires an extension the other side does not advertise SHOULD surface an actionable error rather than failing opaquely. The error SHOULD identify the required extension so the operator or developer can act on it. For HTTP-based interactions the appropriate core error response and status code apply per §22 Error Handling and Error Codes; an implementation that mandates an extension MAY refuse the interaction outright when that extension is not active.
- **Unknown extension identifiers MUST be ignored.** A peer that encounters, in the other side's `extensions` map, an identifier it does not recognize MUST ignore that entry and MUST NOT treat its presence as an error. Such an identifier simply does not enter the active set (the intersection of §24.3).

Extension authors SHOULD document the expected fallback behavior for the case where the extension is not active, so that implementers know what core behavior to provide.

### §24.8 Extensions Defined by This Document

This document defines two extensions built on the framework above:

- **§25 The Tasks Extension** — asynchronous, long-running operations with polling (`tasks/get`), mid-flight client input (`tasks/update`), and cooperative cancellation (`tasks/cancel`), via a durable task handle [SEP-2663].
- **§26 The Interactive User-Interface Extension** — host-rendered interactive user-interface content emitted by tools, with a transport-neutral UI-to-host message dialect [SEP-1865].

The following example shows the capabilities portion of an initialization in which a peer advertises core capabilities together with two extensions — one carrying a settings object and one enabled with no settings:

```json
{
  "capabilities": {
    "tools": {},
    "extensions": {
      "io.modelcontextprotocol/ui": {
        "mimeTypes": ["text/html;profile=mcp-app"]
      },
      "io.modelcontextprotocol/tasks": {}
    }
  }
}
```

In this example the `io.modelcontextprotocol/ui` extension is advertised with a settings object declaring the UI MIME types the peer accepts, and the `io.modelcontextprotocol/tasks` extension is advertised with an empty settings object, meaning it is enabled with no settings. Either extension becomes ACTIVE only if the other peer also advertises the same identifier (§24.3).

## §25 The Tasks Extension

The Tasks extension, identified by `io.modelcontextprotocol/tasks`, models long-running operations as durable, pollable tasks rather than as blocking request/response exchanges. It is opt-in and is negotiated through the extension mechanism of §24 The Extension Mechanism.

### §25.1 Overview and Extension Identifier

The Tasks extension models long-running, server-handled operations as durable, pollable **tasks** rather than as blocking request/response exchanges [SEP-2663]. A server that would otherwise hold a connection open until work completes instead returns an opaque **task handle** immediately, and the client retrieves the eventual outcome by polling. This avoids long-lived connections, survives client disconnects and restarts, exposes progress, and allows the operation to pause for mid-flight input — all without requiring unsolicited server-to-client requests [SEP-2663].

The extension is identified by the exact, case-sensitive string:

```
io.modelcontextprotocol/tasks
```

This identifier is the key used in the extensions capability map (see §6 Capabilities and Extensions) and is governed by the negotiation and gating rules of the extension mechanism (see §24 The Extension Mechanism). All Tasks methods, results, and notifications in this extension are defined inline below.

A conforming implementation MUST treat the identifier as an opaque, exact string and MUST NOT match it case-insensitively or by prefix.

### §25.2 Capability Declaration and Negotiation

Both client and server declare support by including the extension identifier in their respective extensions capability maps. The value associated with the identifier is an OPTIONAL settings object; this extension defines no settings, so the value is an empty object `{}`. Receivers MUST ignore unrecognized members of the settings object.

```ts
// The settings value for the Tasks extension. No settings are defined.
type TasksExtensionCapability = { [key: string]: never }
```

A **client** declares the ability to drive tasks (to poll, to supply input, and to request cancellation) by including `"io.modelcontextprotocol/tasks"` in the `extensions` map of the client capabilities it provides per request, as defined in §6 Capabilities and Extensions and §4 Request Metadata and the Stateless Model. Because the protocol is stateless and per-request (see §4 Request Metadata and the Stateless Model), a client that wishes any given request to be eligible for task augmentation MUST include this declaration in that request's capabilities.

```json
{
  "params": {
    "_meta": {
      "io.modelcontextprotocol/clientCapabilities": {
        "extensions": {
          "io.modelcontextprotocol/tasks": {}
        }
      }
    }
  }
}
```

A **server** declares the ability to return task handles, and to answer the Tasks methods defined below, by including `"io.modelcontextprotocol/tasks"` in the `extensions` map of the capabilities it advertises during discovery, as defined in §5 Protocol Revision, Version Negotiation, and Discovery and §6 Capabilities and Extensions.

```json
{
  "capabilities": {
    "extensions": {
      "io.modelcontextprotocol/tasks": {}
    }
  }
}
```

Negotiation and gating rules:

- A server MUST NOT return a task handle (a result with `resultType` equal to `"task"`, defined below) to a request whose declared client capabilities do not include `"io.modelcontextprotocol/tasks"`. The client opts in once per request via the capability; the server then decides per request whether to produce a task.
- A client that declares the capability MUST be prepared to receive either the request's ordinary result shape or a task handle in its place (see §25.3 Task Augmentation of Existing Requests).
- If a client invokes one of the Tasks methods (`tasks/get`, `tasks/update`, `tasks/cancel`) against a server that has not advertised this extension, or invokes a Tasks method the server cannot service, the server MUST respond with the missing-capability error condition defined in §22 Error Handling and Error Codes.
- A server MUST NOT require any per-call flag or warmup step beyond the per-request capability declaration; task creation is server-directed [SEP-2663].

### §25.3 Task Augmentation of Existing Requests

When the extension is active for a request (the client has declared the capability and the server has advertised it), the server MAY satisfy certain server-handled requests by returning a task handle **instead of** the request's direct result. This applies to tool calls (see §16 Tools) and, at the server's discretion, to any other server-handled request the server chooses to make long-running.

The substitution is signaled through the open result discriminator `resultType` defined in §3 Base Message Format. A task handle is a `Result` whose `resultType` is the literal string `"task"`. Receivers dispatch on `resultType`: a value of `"task"` means the payload is a `CreateTaskResult` (defined below) rather than the request's ordinary result.

A server MAY perform this substitution unsolicited for any individual eligible request; the client does not opt in per call, only once per request via the capability. A client that has declared the capability MUST inspect `resultType` on each eligible response and handle the `"task"` case.

`CreateTaskResult` is a `Result` that also carries the fields of a `Task` (defined below):

```ts
// A Result whose resultType is "task" (resultType is the open
// discriminator from §3 Base Message Format). It carries an
// initial Task object describing the newly created task handle.
type CreateTaskResult = Result & Task & {
  resultType: "task"
}
```

All `Task` fields described in the next subsection appear directly on `CreateTaskResult`. The `_meta` member permitted on any `Result` (see §3 Base Message Format and §14 Common Data Types) is permitted here.

### §25.4 Task and DetailedTask Object Types

A **Task** is the handle and status record for a long-running operation. Its fields:

```ts
type Task = {
  // Opaque, server-minted identifier for this task. REQUIRED.
  // Treated as an opaque string by the client; the client MUST NOT
  // parse or derive meaning from its contents.
  taskId: string

  // Current lifecycle state. REQUIRED. See TaskStatus below.
  status: TaskStatus

  // Human-readable, OPTIONAL description of the current state or
  // progress (for example, "Processing item 42 of 100"). For display
  // only; carries no protocol semantics.
  statusMessage?: string

  // Timestamp at which the task was created. REQUIRED.
  // An ISO 8601 / RFC 3339 date-time string.
  createdAt: string

  // Timestamp at which the task state was last modified. REQUIRED.
  // An ISO 8601 / RFC 3339 date-time string.
  lastUpdatedAt: string

  // Task lifetime in milliseconds, measured from creation. REQUIRED.
  // A non-negative number, or null. The value null means the task
  // has no bounded lifetime (unbounded). After a non-null ttlMs has
  // elapsed, a server MAY discard the task and subsequently answer
  // queries for it with the not-found error of §22 Error Handling
  // and Error Codes.
  ttlMs: number | null

  // Recommended MINIMUM interval, in milliseconds, that a client
  // SHOULD wait between successive tasks/get polls for this task.
  // OPTIONAL. A non-negative number. When absent, the client chooses
  // a reasonable interval. Clients SHOULD NOT poll more frequently
  // than this value.
  pollIntervalMs?: number
}
```

A **DetailedTask** is a `Task` that additionally conveys the terminal payload (or pending input requests) inline, and is the shape returned by `tasks/get`. It is the union of the following variants, discriminated by `status`:

```ts
// status === "working": no additional fields.
type WorkingTask = Task & {
  status: "working"
}

// status === "input_required": carries the outstanding server
// requests the client must fulfill before the task can continue.
type InputRequiredTask = Task & {
  status: "input_required"
  inputRequests: InputRequests
}

// status === "completed": carries the underlying success result —
// exactly what the original request would have returned directly.
type CompletedTask = Task & {
  status: "completed"
  result: { [key: string]: unknown }
}

// status === "failed": carries the underlying error.
type FailedTask = Task & {
  status: "failed"
  error: { [key: string]: unknown }
}

// status === "cancelled": no additional fields.
type CancelledTask = Task & {
  status: "cancelled"
}

type DetailedTask =
  | WorkingTask
  | InputRequiredTask
  | CompletedTask
  | FailedTask
  | CancelledTask
```

On input-bearing variants the `inputRequests` map uses the `InputRequests` type defined in §11.2 (`[key: string]: InputRequest`), and the `inputResponses` map uses the `InputResponses` type defined in §11.4/§25.8; they key outstanding input requests (such as elicitations, see §20 Elicitation) by opaque string so the client can return matching responses.

The `result` field on the `completed` variant is the verbatim ordinary result object the augmented request would have produced had it not been turned into a task — including that result's own `resultType` discriminator and any `_meta` (see §3 Base Message Format). The `error` field on the `failed` variant is a JSON-RPC error object as defined in §22 Error Handling and Error Codes.

### §25.5 Task Status Lifecycle

The `status` field takes one of exactly these five case-sensitive string values:

```ts
type TaskStatus =
  | "working"
  | "input_required"
  | "completed"
  | "failed"
  | "cancelled"
```

| Value | Meaning | Terminal |
| --- | --- | --- |
| `working` | The operation is in progress. | No |
| `input_required` | The server requires client input before it can continue. The `inputRequests` map (on the `input_required` variant of `DetailedTask`) names the outstanding requests; the client fulfills them via `tasks/update`. | No |
| `completed` | The operation finished successfully. The underlying result is conveyed inline in the `result` field of the `completed` variant returned by `tasks/get`. | Yes |
| `failed` | A JSON-RPC error occurred during execution. The error is conveyed inline in the `error` field of the `failed` variant returned by `tasks/get`. | Yes |
| `cancelled` | The operation ended in response to a cancellation request. | Yes |

Rules:

- The states `completed`, `failed`, and `cancelled` are **terminal**. Once a task reaches a terminal state its `status`, and the inline `result` or `error` it carries, are **immutable**; the task MUST NOT subsequently transition to any other state.
- A task in `working` MAY transition to any of `input_required`, `completed`, `failed`, or `cancelled`. A task in `input_required` MAY transition back to `working` (typically after `tasks/update` supplies the needed input) or to any terminal state.
- The underlying outcome is conveyed ONLY once the task is terminal, and ONLY inline within the `DetailedTask` returned by `tasks/get`: `result` for `completed`, `error` for `failed`. A non-terminal `DetailedTask` carries neither `result` nor `error`.
- A client SHOULD continue polling (subject to `pollIntervalMs`) until the task reaches a terminal state.

### §25.6 Durability and Statelessness

Tasks MUST behave correctly under the stateless, instance-agnostic request model of §4 Request Metadata and the Stateless Model:

- Before returning a `CreateTaskResult` (the task handle), a server MUST persist the task durably. The task and its `taskId` MUST survive the completion of the request that created it, client disconnects, and client restarts.
- Any server instance that receives a subsequent `tasks/get`, `tasks/update`, or `tasks/cancel` for a given `taskId` MUST be able to answer it from the durable record; resolution MUST NOT depend on the request landing on the same instance that created the task.
- A server MUST NOT rely on a persistent connection or session affinity to track task state. Where a task needs to carry resumable state across requests, the server MAY reuse the opaque continuation token mechanism of §11 Multi-Round-Trip Requests to encode that state.
- When a non-null `ttlMs` has elapsed, a server MAY discard the task. After discarding it, the server MUST answer queries for that `taskId` with the not-found error condition defined in §22 Error Handling and Error Codes.
- A `taskId` is server-minted and opaque to the client. Clients SHOULD persist `taskId` values durably so that polling can resume after a crash or restart.

### §25.7 Retrieving a Task: tasks/get

A client retrieves the current state of a task by sending a `tasks/get` request. This is the polling primitive of the Tasks extension: a client repeatedly issues `tasks/get` to observe a task's progress until it reaches a terminal status [MCP][SEP-2663].

#### Request

The request method is the literal string `tasks/get`. Its params object carries a single member identifying the task to query.

```ts
interface GetTaskRequest {
  method: "tasks/get";
  params: {
    taskId: string;   // REQUIRED. Identifier of the task to query.
  };
}
```

- `taskId` (REQUIRED, `string`): The server-generated identifier of the task, as obtained from the `taskId` of the `CreateTaskResult` that originally created the task (§25.3 Task Augmentation of Existing Requests, §25.4 Task and DetailedTask Object Types). A client MUST send the value verbatim.

The request MUST be issued by a client that has negotiated the `io.modelcontextprotocol/tasks` extension capability (capability negotiation is defined in §6 Capabilities and Extensions). A server receiving `tasks/get` from a client that did not declare the extension capability MUST respond with the error code `-32003` (Missing Required Client Capability) (see §22 Error Handling and Error Codes).

#### Result

The result is a standard result shape (the discriminator `resultType` is defined in §3 Base Message Format) whose `resultType` member MUST be the literal string `"complete"`. The result body is a `DetailedTask`: a `Task` whose status-specific payload fields are inlined for the task's current status.

```ts
type GetTaskResult = Result & DetailedTask;
```

The `DetailedTask` is one of the following variants, selected by the value of `status`. The base `Task` fields (`taskId`, `status`, `statusMessage`, `createdAt`, `lastUpdatedAt`, `ttlMs`, `pollIntervalMs`) are defined in §25.4 Task and DetailedTask Object Types and are present on every variant.

```ts
type DetailedTask =
  | WorkingTask          // status: "working"
  | InputRequiredTask    // status: "input_required"; adds inputRequests
  | CompletedTask        // status: "completed"; adds result
  | FailedTask           // status: "failed"; adds error
  | CancelledTask;       // status: "cancelled"
```

On receiving `tasks/get`, the server MUST inspect the task's current status and return the matching variant [MCP][SEP-2663]:

1. If the status is `"working"`, the server MUST return a `DetailedTask` with `status: "working"` and no status-specific payload field.
2. If the status is `"input_required"`, the server MUST return a `DetailedTask` with `status: "input_required"` and an `inputRequests` object. The `inputRequests` object MUST contain every server-to-client request that is currently outstanding for the task and that the client must fulfill before the task can proceed.
3. If the status is `"completed"`, the server MUST return a `DetailedTask` with `status: "completed"` and a `result` object containing the final result of the underlying request — the same value the original request would have produced had it returned synchronously.
4. If the status is `"failed"`, the server MUST return a `DetailedTask` with `status: "failed"` and an `error` object containing the JSON-RPC error that occurred during execution.
5. If the status is `"cancelled"`, the server MUST return a `DetailedTask` with `status: "cancelled"` and no status-specific payload field.

#### Polling Semantics

A client observes a task's progress by polling `tasks/get`:

- A client SHOULD honor the `pollIntervalMs` value most recently observed for the task as the minimum interval between consecutive `tasks/get` requests for that task; the client SHOULD NOT poll more frequently than `pollIntervalMs` milliseconds. The value of `pollIntervalMs` MAY change over the lifetime of the task, and a client SHOULD adopt the latest value it observes.
- A server MAY rate-limit a client that polls more frequently than the most recently advertised `pollIntervalMs`.
- A client SHOULD continue polling until the task reaches a terminal status (`"completed"`, `"failed"`, or `"cancelled"`) or until the client issues `tasks/cancel`.
- A client SHOULD persist task identifiers to durable storage so that polling can resume after a client crash or restart; a task identifier is a durable handle and a `tasks/get` for it resolves independently of the connection on which the task was created.

#### Unknown taskId

If the `taskId` does not correspond to a task known to the server — including a task that never existed and a task that has been expired and removed — the server MUST respond to `tasks/get` with a JSON-RPC error whose `code` is `-32602` (Invalid params), rather than with a result (see §22 Error Handling and Error Codes) [MCP][SEP-2663][JSONRPC2]. The `message` is informative and non-normative. A client SHOULD treat a `-32602` response to `tasks/get` as evidence that the task is terminal and unavailable (see §25.11 Task Lifecycle and Cleanup).

```json
{
  "jsonrpc": "2.0",
  "id": 70,
  "error": {
    "code": -32602,
    "message": "Failed to retrieve task: Task not found"
  }
}
```

### §25.8 Supplying Input to a Task: tasks/update

When a task is in the `"input_required"` status, the `tasks/get` result carries an `inputRequests` object: a map of outstanding server-to-client requests (for example elicitation or sampling requests) keyed by arbitrary server-chosen identifiers. A client resolves these by sending one or more `tasks/update` requests carrying the corresponding responses [MCP][SEP-2663].

#### Request

The request method is the literal string `tasks/update`. Its params object carries the task identifier and a map of input responses.

```ts
interface UpdateTaskRequest {
  method: "tasks/update";
  params: {
    taskId: string;                  // REQUIRED. Identifier of the task to update.
    inputResponses: InputResponses;  // REQUIRED. Responses keyed by inputRequests key.
  };
}

interface InputResponses {
  [key: string]: InputResponse;   // key MUST match a currently-outstanding inputRequests key
}
```

- `taskId` (REQUIRED, `string`): The identifier of the task whose outstanding input is being supplied.
- `inputResponses` (REQUIRED, object): A map whose keys correspond to keys present in the task's `inputRequests`, and whose values are the responses to those requests. Each value is shaped as the response to the corresponding server-to-client request would be when surfaced inline (the input-request/response model is defined in §11 Multi-Round-Trip Requests). For example, the response to an elicitation request uses the elicitation result shape defined in §20 Elicitation.

A client MUST issue `tasks/update` only over the negotiated `io.modelcontextprotocol/tasks` extension capability; a server receiving `tasks/update` from a client that did not declare the extension capability MUST respond with error code `-32003` (Missing Required Client Capability) (see §22 Error Handling and Error Codes).

Key rules for input responses:

- Each key in a task's `inputRequests` MUST be unique over the entire lifetime of the task. A server MUST NOT reuse a key for a subsequent server-to-client request after a response for that key has been delivered, and MUST NOT use the same key to denote two distinct requests during the task's lifetime. This guarantees that an `inputResponses` entry keyed by a given identifier unambiguously refers to the request the client intends to answer.
- A server SHOULD ignore any `inputResponses` entry whose key is not currently outstanding for the task. This includes keys that were never issued, keys that have already been answered, and keys whose corresponding request has been superseded.
- A server MAY accept a partial set of responses — a strict subset of the currently-outstanding keys. In that case the task remains in `"input_required"` until the remaining responses arrive.
- A client SHOULD track which `inputRequests` keys it has already answered so that it does not respond to the same request more than once; `inputRequests` is a point-in-time snapshot and the same key MAY appear on multiple consecutive `tasks/get` results until its response is processed.

#### Result

The result is an empty acknowledgement. Its `resultType` member MUST be the literal string `"complete"`.

```ts
type UpdateTaskResult = Result;   // empty acknowledgement
```

On success the server MUST acknowledge with this empty result. The acknowledgement is eventually consistent: the server MAY accept the responses and return the acknowledgement before the task's observable status (via `tasks/get` or via `notifications/tasks`) reflects them. If the `taskId` does not correspond to a known task, the server SHOULD respond with error code `-32602` (Invalid params) (see §22 Error Handling and Error Codes).

After sending `tasks/update`, a client SHOULD continue observing the task — by polling `tasks/get` or via `notifications/tasks` — until the task reaches a terminal status.

### §25.9 Cancelling a Task: tasks/cancel

A client requests cooperative cancellation of an in-progress task by sending a `tasks/cancel` request. The general-purpose `notifications/cancelled` notification (defined in §15 Utilities: Progress, Cancellation, Logging, and Trace Context) MUST NOT be used to cancel a task; `tasks/cancel` is the only mechanism for task cancellation [MCP][SEP-2663].

#### Request

The request method is the literal string `tasks/cancel`. Its params object carries a single member identifying the task.

```ts
interface CancelTaskRequest {
  method: "tasks/cancel";
  params: {
    taskId: string;   // REQUIRED. Identifier of the task to cancel.
  };
}
```

- `taskId` (REQUIRED, `string`): The identifier of the task to cancel.

A client MUST issue `tasks/cancel` only over the negotiated `io.modelcontextprotocol/tasks` extension capability; a server receiving `tasks/cancel` from a client that did not declare the extension capability MUST respond with error code `-32003` (Missing Required Client Capability) (see §22 Error Handling and Error Codes).

#### Result

The result is an empty acknowledgement. Its `resultType` member MUST be the literal string `"complete"`.

```ts
type CancelTaskResult = Result;   // empty acknowledgement
```

On success the server MUST acknowledge with this empty result. If the `taskId` does not correspond to a known task, the server SHOULD respond with error code `-32602` (Invalid params) (see §22 Error Handling and Error Codes).

Cancellation semantics:

- Cancellation is **cooperative**. The request signals intent; the server decides whether and when to honor it. A server is not obligated to stop the work — it is only obligated to acknowledge the request. Eventual transition to the `"cancelled"` status is not guaranteed.
- Cancellation processing is eventually consistent. After the acknowledgement, the task's observable status MAY remain `"working"` (or another non-terminal status) for a time, and the task MAY ultimately reach a terminal status other than `"cancelled"` if the work finished before cancellation could take effect.
- The server transitions the task toward the `"cancelled"` terminal status as soon as feasible when it does honor the request.
- A task that has already reached a terminal status (`"completed"`, `"failed"`, or `"cancelled"`) MUST NOT change status as a result of `tasks/cancel`; terminal status is final.
- A client MAY drop all local state associated with a task as soon as it sends `tasks/cancel` — for example, the set of `inputRequests` keys it has already answered. A client need not poll `tasks/get` again to wait for the task to reach `"cancelled"`.

### §25.10 Task Status Notifications: notifications/tasks

In addition to servicing client polls, a server MAY push task state changes to a client through the `notifications/tasks` notification. Each such notification carries a complete `DetailedTask` for the task's current status — identical to what `tasks/get` would have returned at that moment — so a client receiving it need not issue an extra `tasks/get` round trip [MCP][SEP-2663].

```ts
type TaskStatusNotificationParams = NotificationParams & DetailedTask;

interface TaskStatusNotification {
  method: "notifications/tasks";
  params: TaskStatusNotificationParams;
}
```

The notification method is the literal string `notifications/tasks`. Its `params` is a `DetailedTask` (the same variant union returned by `tasks/get`) optionally carrying the common notification metadata of §3 Base Message Format. The `params` therefore always includes `taskId` and `status`, plus the status-specific payload field (`inputRequests`, `result`, or `error`) for the `"input_required"`, `"completed"`, and `"failed"` statuses respectively.

#### Opt-in via subscription

Delivery of `notifications/tasks` is OPT-IN through the subscription mechanism of §10 Server-to-Client Streaming and Subscriptions. A client opts in by including the task identifiers it is interested in, as a `taskIds` filter, when it issues `subscriptions/listen`:

```ts
interface SubscriptionsListenRequest {
  method: "subscriptions/listen";
  params: {
    notifications: {
      taskIds?: string[];   // task IDs to receive notifications/tasks for
      // other subscription filter fields, per §10
    };
  };
}
```

- `taskIds` (OPTIONAL, `string[]`): The list of task identifiers for which the client wishes to receive `notifications/tasks`. Each element MUST be a `taskId` the client holds.

In its subscription acknowledgement (the `notifications/subscriptions/acknowledged` notification, defined in §10 Server-to-Client Streaming and Subscriptions), the server includes the subset of task identifiers it has agreed to send status notifications for, under a `taskIds` member of the same `notifications` object.

Rules:

- A server MUST NOT push `notifications/tasks` for any task the client did not subscribe to via a `taskIds` filter on `subscriptions/listen`.
- If a client requests task status notifications (supplies `taskIds`) but has not negotiated the `io.modelcontextprotocol/tasks` extension capability, the server MUST respond to `subscriptions/listen` with error code `-32003` (Missing Required Client Capability) (see §22 Error Handling and Error Codes).
- Polling via `tasks/get` remains available regardless of whether notifications are in use. A client MAY rely solely on notifications, MAY rely solely on polling, or MAY combine the two; a client that has subscribed need not poll.
- The `notifications/progress` and `notifications/message` notifications (defined in §15 Utilities: Progress, Cancellation, Logging, and Trace Context) MUST NOT be sent for a task; task state is conveyed only via `tasks/get` and `notifications/tasks`.

#### Relationship to Multi-Round-Trip Input

A task in the `"input_required"` status represents the same need for client-provided input that §11 Multi-Round-Trip Requests expresses, surfaced through the task lifecycle rather than as a retry of the originating method [MCP][SEP-2663].

The two mechanisms are distinct in how the input is surfaced and resolved:

- **Before** a task exists — when a server needs client input in order to decide whether to proceed, or to produce its response — the server uses the in-line multi-round-trip flow of §11 Multi-Round-Trip Requests on the original request: it returns the input-required result, and the client re-issues the same method carrying its input responses inline. A server SHOULD resolve such pre-task exchanges synchronously before returning a `CreateTaskResult`.
- **During** task execution — when a running task needs client input — the server surfaces the outstanding requests in the `inputRequests` field of the `tasks/get` result (and of any `notifications/tasks`), and the client resolves them with `tasks/update` carrying `inputResponses`, NOT by re-issuing the original method.

Each entry in `inputRequests` MUST be treated by the client exactly as it would treat the equivalent standalone server-to-client request: an elicitation request surfaced via `inputRequests` is subject to the same trust model and user-facing behavior as a direct elicitation request (see §20 Elicitation), and a task is not a higher-trust channel.

For a single outstanding input, the in-line retry mechanism of §11 Multi-Round-Trip Requests and the `tasks/update` mechanism MUST NOT be mixed: input surfaced through a task's `inputRequests` is resolved only via `tasks/update`, and input surfaced through an in-line input-required result is resolved only via re-issuing the original method.

### §25.11 Task Lifecycle and Cleanup

A task is a durable state machine. It is created in a non-terminal status (typically `"working"`) and transitions among `"working"` and `"input_required"` until it reaches one of the terminal statuses `"completed"`, `"failed"`, or `"cancelled"`, after which its status is final and does not change [MCP][SEP-2663].

Expiry and removal:

- A task carries a `ttlMs` field: a time-to-live duration in integer milliseconds measured from `createdAt`, or `null` for unlimited. The value MAY change over the lifetime of the task.
- A server MAY mark a task `"failed"` at any point after `ttlMs` has elapsed, and MAY subsequently remove the task at any time. A server is not required to retain tasks indefinitely.
- A client MAY treat a non-null `ttlMs` as a backstop: if the task's observable status has not advanced by the time `createdAt` plus `ttlMs` has elapsed, the client MAY consider the task not usable.
- After a task has been expired and removed, a `tasks/get` for its `taskId` MUST return the `-32602` (Invalid params) error described above. A client SHOULD treat an unknown `taskId` — whether because the task never existed or because it has expired — as a terminal, unavailable task and stop polling it.

Execution errors:

- When the underlying request encounters a JSON-RPC protocol error during execution, the task moves to the `"failed"` status. The `tasks/get` result MUST include the `error` field carrying that JSON-RPC error, and SHOULD include a `statusMessage` with diagnostic information.
- The `"failed"` status MUST NOT be used for non-protocol faults. A request that completes at the protocol level but conveys an application-level error within its result — for example a tool result carrying `isError: true` — MUST be reported with the `"completed"` status, with the error details carried inside the `result` field. This preserves a strict separation between protocol-level faults (`"failed"`) and application-level outcomes (`"completed"`).

### §25.12 Examples

A `CreateTaskResult` returned in place of a tool call's ordinary result. The `resultType` value `"task"` signals the substitution (see §3 Base Message Format):

```json
{
  "jsonrpc": "2.0",
  "id": 7,
  "result": {
    "resultType": "task",
    "taskId": "task_3f9a2c1e8b7d4f60",
    "status": "working",
    "statusMessage": "Queued: building deployment pipeline",
    "createdAt": "2026-07-28T14:03:12Z",
    "lastUpdatedAt": "2026-07-28T14:03:12Z",
    "ttlMs": 3600000,
    "pollIntervalMs": 2000
  }
}
```

A `Task` object in the `working` state carrying a bounded `ttlMs` and a recommended `pollIntervalMs`:

```json
{
  "taskId": "task_3f9a2c1e8b7d4f60",
  "status": "working",
  "statusMessage": "Processing item 42 of 100",
  "createdAt": "2026-07-28T14:03:12Z",
  "lastUpdatedAt": "2026-07-28T14:05:47Z",
  "ttlMs": 3600000,
  "pollIntervalMs": 2000
}
```

#### tasks/get request

```json
{
  "jsonrpc": "2.0",
  "id": 8,
  "method": "tasks/get",
  "params": {
    "taskId": "786512e2-9e0d-44bd-8f29-789f320fe840"
  }
}
```

#### tasks/get result: completed task with inline result

```json
{
  "jsonrpc": "2.0",
  "id": 8,
  "result": {
    "resultType": "complete",
    "taskId": "786512e2-9e0d-44bd-8f29-789f320fe840",
    "status": "completed",
    "createdAt": "2026-07-28T10:30:00Z",
    "lastUpdatedAt": "2026-07-28T10:50:00Z",
    "ttlMs": 3600000,
    "pollIntervalMs": 5000,
    "result": {
      "content": [
        {
          "type": "text",
          "text": "Hello, Luca!"
        }
      ],
      "isError": false
    }
  }
}
```

#### tasks/update request supplying input

This resolves an outstanding elicitation request that was surfaced under the `inputRequests` key `"name"` while the task was in the `"input_required"` status. The response value uses the elicitation result shape of §20 Elicitation.

```json
{
  "jsonrpc": "2.0",
  "id": 6,
  "method": "tasks/update",
  "params": {
    "taskId": "786512e2-9e0d-44bd-8f29-789f320fe840",
    "inputResponses": {
      "name": {
        "action": "accept",
        "content": {
          "input": "Luca"
        }
      }
    }
  }
}
```

The server acknowledges with an empty result:

```json
{
  "jsonrpc": "2.0",
  "id": 6,
  "result": {
    "resultType": "complete"
  }
}
```

#### tasks/cancel request

```json
{
  "jsonrpc": "2.0",
  "id": 9,
  "method": "tasks/cancel",
  "params": {
    "taskId": "786512e2-9e0d-44bd-8f29-789f320fe840"
  }
}
```

The server acknowledges with an empty result:

```json
{
  "jsonrpc": "2.0",
  "id": 9,
  "result": {
    "resultType": "complete"
  }
}
```

#### notifications/tasks notification

A server that has accepted a subscription for this task identifier pushes the full `DetailedTask` for the new status:

```json
{
  "jsonrpc": "2.0",
  "method": "notifications/tasks",
  "params": {
    "taskId": "786512e2-9e0d-44bd-8f29-789f320fe840",
    "status": "completed",
    "createdAt": "2026-07-28T10:30:00Z",
    "lastUpdatedAt": "2026-07-28T10:50:00Z",
    "ttlMs": 60000,
    "pollIntervalMs": 5000,
    "result": {
      "content": [
        {
          "type": "text",
          "text": "Operation completed successfully."
        }
      ],
      "isError": false
    }
  }
}
```

## §26 The Interactive User-Interface Extension

This section defines the Interactive User-Interface extension (the "apps" extension), an OPTIONAL extension to the Model Context Protocol by which a server associates interactive HTML user-interface components with its tools, and a host renders those components inline and exchanges messages with them over a host-provided channel [SEP-1865][MCP]. This section builds on the general extension framework of §24 The Extension Mechanism, the capability negotiation of §6 Capabilities and Extensions, the tool model of §16 Tools, and the resource model of §17 Resources.

A reader implementing only the server-side SDK obligations needs §26.1 through §26.4 and §26.9. A reader implementing a host needs all subsections, in particular §26.5 through §26.8.

### §26.1 Purpose and Roles

A server MAY declare that one or more of its tools have an associated interactive user interface. The interface is an HTML document, served as a resource (§17 Resources), that the host renders in an isolated, sandboxed browsing context positioned inline with the conversation. The rendered interface communicates with the host over a host-provided message channel using the JSON-RPC-style dialect defined in §26.5, by which it reads the tool input and result it was rendered for, requests that the host invoke tools on its behalf (subject to host mediation and user consent, §26.7), and receives lifecycle notifications [SEP-1865].

The division of responsibilities is fixed and normative:

- A server (and a server-side SDK) is RESPONSIBLE for: declaring the UI association on a tool via the reserved metadata key defined in §26.3; and serving the UI resource at its `ui://` URI through the standard `resources/read` mechanism (§17 Resources, §26.4). A server SDK is NOT responsible for rendering, sandboxing, or running the message-channel dialect.
- A host (and a host/client implementation) is RESPONSIBLE for: rendering the UI in a sandboxed, isolated browsing context (§26.4); enforcing the content-security policy and permissions (§26.4, §26.7); running the message-channel dialect (§26.5); and mediating and obtaining user consent for any action the UI requests (§26.7).

The UI rendering is a host responsibility. A conforming server SDK MUST be implementable without any rendering, browser, or UI-toolkit dependency.

### §26.2 Extension Identifier and Capability Negotiation

The extension identifier is the exact string:

```
io.modelcontextprotocol/ui
```

This identifier is used as the key under the `extensions` capability map defined in §6 Capabilities and Extensions and gated per §24 The Extension Mechanism. The extension is active for a session only if it is negotiated through that map. A receiver MUST treat the identifier as an opaque, case-sensitive string.

A host (client) that supports rendering interactive user interfaces MUST advertise the extension in the `extensions` map of the `io.modelcontextprotocol/clientCapabilities` it carries in the `_meta` of every request (§4 Request Metadata and the Stateless Model, §6 Capabilities and Extensions, §24.4). The advertised value is an object with the following field:

```ts
interface UiHostExtensionCapability {
  mimeTypes: string[]; // REQUIRED. The UI resource MIME types the host can render.
}
```

- `mimeTypes`: REQUIRED. An array of MIME type strings the host is able to render as interactive user interfaces. A host that supports this extension MUST include the exact string `"text/html;profile=mcp-app"` in this array. The string is matched verbatim and case-sensitively, including the `;profile=mcp-app` profile parameter and the absence of surrounding whitespace.

A server MUST NOT declare UI associations on tools, and MUST NOT expect any UI resource to be rendered, unless the host has advertised this extension with a `mimeTypes` array that includes `"text/html;profile=mcp-app"`. A server MAY still expose the underlying tools as ordinary tools (with no rendered UI) when the host has not negotiated the extension; in that case the host treats the tool as a normal tool per §16 Tools and ignores the UI metadata key per §24 The Extension Mechanism.

Example host advertisement inside a `server/discover` request:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "server/discover",
  "params": {
    "_meta": {
      "io.modelcontextprotocol/protocolVersion": "2026-07-28",
      "io.modelcontextprotocol/clientInfo": { "name": "ExampleHost", "version": "1.0.0" },
      "io.modelcontextprotocol/clientCapabilities": {
        "extensions": {
          "io.modelcontextprotocol/ui": {
            "mimeTypes": ["text/html;profile=mcp-app"]
          }
        }
      }
    }
  }
}
```

A server that supports the extension acknowledges it in its `server/discover` result (`DiscoverResult`) by including the same identifier key under `capabilities.extensions`, per §5.3 Discovery, §6 Capabilities and Extensions, and §24 The Extension Mechanism. The acknowledged value is an object; it MAY be empty:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "resultType": "complete",
    "supportedVersions": ["2026-07-28"],
    "capabilities": {
      "extensions": {
        "io.modelcontextprotocol/ui": {}
      }
    },
    "serverInfo": { "name": "ExampleServer", "version": "1.0.0" }
  }
}
```

### §26.3 Declaring a UI on a Tool

A tool declares its associated user interface using a reserved key in the tool's `_meta` object (the metadata mechanism of §24 The Extension Mechanism, applied to the tool shape of §16 Tools). The exact nested key path is `_meta.ui`, an object with the following fields:

```ts
interface ToolUiMeta {
  resourceUri: string;                  // REQUIRED. ui:// URI of the UI resource.
  visibility?: ("model" | "app")[];     // OPTIONAL. Default ["model", "app"].
}
```

- `resourceUri`: REQUIRED. A string URI that MUST use the `ui://` scheme (§26.4) and that identifies the UI resource to render for this tool. The host obtains the resource by issuing `resources/read` for this exact URI (§17 Resources, §26.4).
- `visibility`: OPTIONAL. An array whose elements are drawn from the exact enum strings `"model"` and `"app"`. When omitted, the value is treated as `["model", "app"]`. The meaning of each value:
  - `"model"`: the tool is visible to, and callable by, the model/agent through the ordinary tool-calling flow (§16 Tools).
  - `"app"`: the tool is callable by the rendered UI through the message-channel dialect (§26.5), subject to host mediation and consent (§26.7).

  A host SHOULD reject a `tools/call` request originating from a rendered UI for a tool whose effective `visibility` array does not include `"app"` (§26.5, §26.7). A tool with `visibility` set to `["app"]` is callable only by the UI and is hidden from the model's tool list.

A receiver that does not negotiate this extension MUST ignore the `_meta.ui` key per §24 The Extension Mechanism; the presence of the key MUST NOT change the behavior of an ordinary `tools/call` (§16 Tools).

Example: a tool with a UI declaration in `_meta.ui`, as returned in a `tools/list` result (§16 Tools):

```json
{
  "name": "get-time",
  "title": "Get Time",
  "description": "Returns the current server time.",
  "inputSchema": {
    "type": "object",
    "properties": {}
  },
  "_meta": {
    "ui": {
      "resourceUri": "ui://get-time/mcp-app.html",
      "visibility": ["model", "app"]
    }
  }
}
```

### §26.4 The UI Resource

A UI resource is an ordinary MCP resource (§17 Resources) identified by a URI in the `ui://` scheme and carrying the UI MIME type. The host obtains it by issuing a `resources/read` request (§17 Resources) for the exact `resourceUri` declared in `_meta.ui` (§26.3). The host MAY preload the UI resource before the associated tool is called.

The `ui://` scheme [RFC3986] designates an MCP interactive-user-interface resource. The authority and path components after `ui://` are server-defined and opaque to the host; the host MUST treat the whole URI as an opaque identifier and MUST NOT derive a network origin from it.

The resource content MUST be served with the MIME type, reproduced verbatim and case-sensitively:

```
text/html;profile=mcp-app
```

The `contents` entry returned by `resources/read` carries the HTML document in its `text` field (or, for a binary-encoded payload, in `blob` as Base64 [RFC4648]), per the resource content shape of §17 Resources. A UI resource's `contents` entry MAY additionally carry presentation and security hints under its own `_meta.ui` object; these hints, when present on the resource, take effect for rendering. The fields are:

```ts
interface ResourceUiMeta {
  csp?: UiContentSecurityPolicy;  // OPTIONAL. Origins the UI may contact/load/frame.
  permissions?: UiPermissions;    // OPTIONAL. Sandbox permissions requested.
  domain?: string;                // OPTIONAL. Dedicated origin to host the UI under.
  prefersBorder?: boolean;        // OPTIONAL. Border presentation preference.
}

interface UiContentSecurityPolicy {
  connectDomains?: string[];   // OPTIONAL. Origins the UI may open network connections to.
  resourceDomains?: string[];  // OPTIONAL. Origins the UI may load scripts/styles/media from.
  frameDomains?: string[];     // OPTIONAL. Origins the UI may embed in nested frames.
  baseUriDomains?: string[];   // OPTIONAL. Origins permitted for the document base URI.
}

interface UiPermissions {
  camera?: {};         // OPTIONAL. Request camera access.
  microphone?: {};     // OPTIONAL. Request microphone access.
  geolocation?: {};    // OPTIONAL. Request geolocation access.
  clipboardWrite?: {}; // OPTIONAL. Request clipboard-write access.
}
```

- `csp`: OPTIONAL. A content-security-policy descriptor [CSP]. Each member is an array of origin strings. `connectDomains` lists origins the UI MAY open network connections to. `resourceDomains` lists origins the UI MAY load resources (scripts, stylesheets, images, media) from. `frameDomains` lists origins the UI MAY embed in nested frames. `baseUriDomains` lists origins permitted as the document base URI. An origin that is not listed in the applicable member MUST be blocked by the host. When `csp` is omitted, the host MUST apply a restrictive deny-by-default policy (§26.7).
- `permissions`: OPTIONAL. An object whose present members request additional sandbox capabilities. Each member name is one of the exact strings `camera`, `microphone`, `geolocation`, `clipboardWrite`; each member value is an empty object `{}`. A member's presence requests that capability; its absence means the capability is not requested. The host MUST NOT grant a capability that is not requested, and MAY decline a requested capability (§26.7).
- `domain`: OPTIONAL. A string naming a dedicated origin under which the host SHOULD render the UI, isolating it from other UI resources.
- `prefersBorder`: OPTIONAL. A boolean expressing the server's preference that the host render a visible border around the UI. The host MAY honor or ignore this preference.

The host MUST render the resource content in a sandboxed, isolated browsing context — a sandboxed iframe or equivalent [HTML-IFRAME] — that denies the content access to the embedding document, its cookies, its storage, and its navigation. The host MUST apply a restrictive content-security policy to the rendered content [CSP], constrained by the declared `csp` descriptor (§26.7). The rendered content MUST NOT be granted ambient access to host state or user data; the only channel between the rendered content and the host is the message-channel dialect defined in §26.5.

Example `resources/read` result returning an `mcp-app` HTML resource:

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "resultType": "complete",
    "contents": [
      {
        "uri": "ui://get-time/mcp-app.html",
        "mimeType": "text/html;profile=mcp-app",
        "text": "<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"UTF-8\"><title>Get Time App</title></head><body><p><strong>Server Time:</strong> <code id=\"server-time\">Loading...</code></p><script type=\"module\" src=\"./mcp-app.js\"></script></body></html>",
        "_meta": {
          "ui": {
            "csp": {
              "connectDomains": ["https://api.example.com"],
              "resourceDomains": ["https://cdn.example.com"]
            },
            "permissions": { "clipboardWrite": {} },
            "prefersBorder": true
          }
        }
      }
    ],
    "ttlMs": 0,
    "cacheScope": "private"
  }
}
```

### §26.5 The UI-to-Host Message Dialect

The rendered UI and the host exchange JSON-RPC 2.0 messages [JSONRPC2] over the host-provided channel (for example, browser message passing between the sandboxed frame and the host document). Each message is a JSON-RPC request, response, or notification with the wire shape defined in §3 Base Message Format. The dialect reuses a small subset of core MCP method names and adds methods under the `ui/` prefix. Method and notification names below are reproduced verbatim and case-sensitively. The "Sender" column states which side originates each message.

The protocol version string carried in the initialization handshake of this dialect is the exact value `"2026-01-26"`. It identifies the message-dialect revision and is independent of the core protocol revision negotiated at the `server/discover` handshake (§5 Protocol Revision, Version Negotiation, and Discovery).

#### §26.5.1 Initialization handshake

The UI initializes the channel. The handshake is: the UI sends a `ui/initialize` request; the host replies with the initialize result; the UI then sends a `ui/notifications/initialized` notification. The UI MUST NOT issue any other dialect message before it has received the response to `ui/initialize`.

`ui/initialize` — request, sent UI → Host:

```ts
interface UiInitializeParams {
  protocolVersion?: string;          // OPTIONAL. Dialect revision the UI implements, e.g. "2026-01-26".
  clientInfo?: { name: string; version: string }; // OPTIONAL. UI identity.
  appCapabilities?: {                // OPTIONAL. Capabilities the UI offers.
    experimental?: {};
    tools?: { listChanged?: boolean };
    availableDisplayModes?: ("inline" | "fullscreen" | "pip")[];
  };
}
```

The host responds with the initialize result, sent Host → UI:

```ts
interface UiInitializeResult {
  protocolVersion: string;           // REQUIRED. Dialect revision, e.g. "2026-01-26".
  hostInfo?: { name: string; version: string };
  hostCapabilities?: {
    experimental?: {};
    openLinks?: {};                  // Present if the host honors ui/open-link.
    serverTools?: { listChanged?: boolean };
    serverResources?: { listChanged?: boolean };
    logging?: {};
    sandbox?: {
      permissions?: UiPermissions;   // Capabilities actually granted to the UI.
      csp?: UiContentSecurityPolicy; // Effective content-security policy applied.
    };
  };
  hostContext?: UiHostContext;       // Initial rendering context (see below).
}

interface UiHostContext {
  toolInfo?: { id?: string | number; tool: object }; // The tool this UI was rendered for (§16 Tools shape).
  theme?: "light" | "dark";
  styles?: {
    variables?: { [key: string]: string }; // Host style variables keyed by name.
    css?: { fonts?: string };
  };
  displayMode?: "inline" | "fullscreen" | "pip";
  availableDisplayModes?: string[];
  containerDimensions?: { height?: number; maxHeight?: number; width?: number; maxWidth?: number };
  locale?: string;
  timeZone?: string;
  userAgent?: string;
  platform?: "web" | "desktop" | "mobile";
  deviceCapabilities?: { touch?: boolean; hover?: boolean };
  safeAreaInsets?: { top: number; right: number; bottom: number; left: number };
}
```

`ui/notifications/initialized` — notification, sent UI → Host, no params; signals the UI is ready to receive tool data.

Example `ui/initialize` request (UI → Host):

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "ui/initialize",
  "params": {
    "protocolVersion": "2026-01-26",
    "clientInfo": { "name": "Get Time App", "version": "1.0.0" },
    "appCapabilities": {
      "availableDisplayModes": ["inline", "fullscreen", "pip"],
      "tools": { "listChanged": true }
    }
  }
}
```

Example initialize result (Host → UI):

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "protocolVersion": "2026-01-26",
    "hostInfo": { "name": "ExampleHost", "version": "1.0.0" },
    "hostCapabilities": {
      "openLinks": {},
      "serverTools": { "listChanged": true },
      "logging": {},
      "sandbox": {
        "permissions": { "clipboardWrite": {} },
        "csp": { "connectDomains": ["https://api.example.com"] }
      }
    },
    "hostContext": {
      "theme": "dark",
      "displayMode": "inline",
      "locale": "en-US",
      "platform": "web",
      "containerDimensions": { "width": 640, "maxHeight": 480 }
    }
  }
}
```

Example initialized notification (UI → Host):

```json
{ "jsonrpc": "2.0", "method": "ui/notifications/initialized" }
```

#### §26.5.2 Tool input and result delivery (Host → UI)

The host delivers to the UI the input and result of the tool the UI was rendered for, via notifications:

`ui/notifications/tool-input` — notification, Host → UI. The complete tool arguments.

```ts
interface ToolInputParams { arguments: { [key: string]: unknown }; }
```

`ui/notifications/tool-input-partial` — notification, Host → UI. OPTIONAL. A partial/streaming snapshot of tool arguments delivered before the complete input. Params shape is identical to `ui/notifications/tool-input`.

`ui/notifications/tool-result` — notification, Host → UI. The result of the tool call. Params carry the tool result shape of §16 Tools:

```ts
interface ToolResultParams {
  content?: object[];                       // Content blocks (§14 Common Data Types).
  structuredContent?: unknown; // any JSON value, per §16
  isError?: boolean;
  _meta?: { [key: string]: unknown };
}
```

`ui/notifications/tool-cancelled` — notification, Host → UI. The associated tool call was cancelled.

```ts
interface ToolCancelledParams { reason: string; }
```

Example tool-result delivery (Host → UI):

```json
{
  "jsonrpc": "2.0",
  "method": "ui/notifications/tool-result",
  "params": {
    "content": [{ "type": "text", "text": "2026-07-28T12:00:00Z" }]
  }
}
```

#### §26.5.3 Tool-invocation and other requests (UI → Host)

The UI MAY request that the host act on its behalf. Each such request is mediated by the host and is subject to user consent where applicable (§26.7).

`tools/call` — request, UI → Host. Asks the host to invoke a server tool on the UI's behalf. The params and result reuse the core tool-call shape of §16 Tools. The host MUST mediate this request: it routes the call to the server, and it MUST obtain user consent and apply its policy before doing so (§26.7). The host SHOULD reject the request if the named tool's effective `visibility` (§26.3) does not include `"app"`.

```ts
interface ToolsCallParams { name: string; arguments?: { [key: string]: unknown }; }
```

`resources/read` — request, UI → Host. Asks the host to read a server resource on the UI's behalf, reusing the core shape of §17 Resources. The host mediates and MAY decline.

`ui/open-link` — request, UI → Host. Asks the host to open an external link. The host MAY decline and SHOULD confirm with the user.

```ts
interface OpenLinkParams { url: string; } // Result: {}
```

`ui/message` — request, UI → Host. Asks the host to insert a message into the conversation on the user's behalf.

```ts
interface UiMessageParams {
  role: "user";
  content: { type: "text"; text: string };
} // Result: {}
```

`ui/request-display-mode` — request, UI → Host. Asks the host to change the display mode. The host MAY grant a different mode than requested; the result reports the mode actually applied.

```ts
interface RequestDisplayModeParams { mode: "inline" | "fullscreen" | "pip"; }
interface RequestDisplayModeResult { mode: "inline" | "fullscreen" | "pip"; }
```

`ui/update-model-context` — request, UI → Host. Supplies structured content from the UI to be incorporated into the model's context for the conversation.

```ts
interface UpdateModelContextParams {
  content?: object[];                       // Content blocks (§14 Common Data Types).
  structuredContent?: unknown; // any JSON value, per §16
} // Result: {}
```

`notifications/message` — notification, UI → Host. A logging message, reusing the core logging shape of §15 Utilities: Progress, Cancellation, Logging, and Trace Context.

`ping` — request, MAY be sent in either direction (UI ↔ Host). It carries no parameters and an empty result; the receiver MUST respond promptly with a success response so the sender can confirm the peer is still live.

```ts
interface PingParams {} // Result: {}
```

Example tool-invocation request from the UI (UI → Host):

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": { "name": "get-time", "arguments": {} }
}
```

#### §26.5.4 Lifecycle and context-change messages (Host → UI)

`ui/notifications/size-changed` — notification, Host → UI. The container size changed.

```ts
interface SizeChangedParams { width: number; height: number; }
```

`ui/notifications/host-context-changed` — notification, Host → UI. One or more host-context fields changed; params carry a partial `UiHostContext` (§26.5.1) containing only the changed members (for example `theme`, `displayMode`, `styles`).

`ui/resource-teardown` — request, Host → UI. Asks the UI to tear down before the host removes it. The UI SHOULD release resources and respond. Result is an empty object `{}`.

```ts
interface ResourceTeardownParams { reason: string; } // Result: {}
```

Example teardown request (Host → UI):

```json
{
  "jsonrpc": "2.0",
  "id": 9,
  "method": "ui/resource-teardown",
  "params": { "reason": "conversation-closed" }
}
```

#### §26.5.5 Host-internal sandbox-proxy messages

On hosts that interpose a sandbox proxy between the host document and the rendered content, two host-internal notifications coordinate handoff of the resource into the sandbox. These are part of the host implementation and are not exchanged with a server.

`ui/notifications/sandbox-proxy-ready` — notification, Sandbox → Host, no params; the sandbox proxy is ready to receive the resource.

`ui/notifications/sandbox-resource-ready` — notification, Host → Sandbox. Delivers the resource HTML and the policy to apply.

```ts
interface SandboxResourceReadyParams {
  html: string;                    // The UI document to render.
  sandbox?: string;                // Sandbox token string to apply, if any.
  csp?: UiContentSecurityPolicy;   // Effective content-security policy.
  permissions?: UiPermissions;     // Granted permissions.
}
```

### §26.6 Method and Notification Name Registry

The complete set of dialect method and notification names, reproduced verbatim, with direction:

| Name | Kind | Sender |
| --- | --- | --- |
| `ui/initialize` | request | UI → Host |
| `ui/notifications/initialized` | notification | UI → Host |
| `ui/notifications/tool-input` | notification | Host → UI |
| `ui/notifications/tool-input-partial` | notification | Host → UI |
| `ui/notifications/tool-result` | notification | Host → UI |
| `ui/notifications/tool-cancelled` | notification | Host → UI |
| `tools/call` | request | UI → Host |
| `resources/read` | request | UI → Host |
| `ui/open-link` | request | UI → Host |
| `ui/message` | request | UI → Host |
| `ui/request-display-mode` | request | UI → Host |
| `ui/update-model-context` | request | UI → Host |
| `notifications/message` | notification | UI → Host |
| `ping` | request | UI ↔ Host |
| `ui/notifications/size-changed` | notification | Host → UI |
| `ui/notifications/host-context-changed` | notification | Host → UI |
| `ui/resource-teardown` | request | Host → UI |
| `ui/notifications/sandbox-proxy-ready` | notification | Sandbox → Host |
| `ui/notifications/sandbox-resource-ready` | notification | Host → Sandbox |

### §26.7 Security and Consent

These requirements are normative for hosts and supplement §28 Security Considerations.

- **Sandboxing.** The host MUST render every UI resource in a sandboxed, isolated browsing context [HTML-IFRAME] that denies the content access to the embedding document's DOM, cookies, storage, and navigation. The rendered content MUST NOT be able to escape the sandbox to reach host or user state.
- **Single channel.** The only path between the rendered UI and the host is the message-channel dialect of §26.5. The host MUST NOT grant the UI ambient access to host or user data through any other path.
- **Content-security policy.** The host MUST apply a content-security policy to the rendered content [CSP]. When the resource declares a `csp` descriptor (§26.4), the host MUST constrain the UI to the declared origins: any origin not present in the applicable `csp` member MUST be blocked. When no `csp` is declared, the host MUST apply a restrictive deny-by-default policy that blocks all external origins except those it explicitly permits. The effective policy the host applied is reported back to the UI in the `hostCapabilities.sandbox.csp` field of the initialize result (§26.5.1).
- **Permissions.** The host MUST NOT grant any sandbox permission (§26.4 `permissions`) that the resource did not request, and MAY decline a requested permission. The set of permissions actually granted is reported in `hostCapabilities.sandbox.permissions` (§26.5.1).
- **Consent for actions.** The host MUST mediate every action the UI requests. For any `tools/call` requested by the UI (§26.5.3), the host MUST apply its tool-execution policy and MUST obtain user consent as it would for any tool invocation, and the host SHOULD reject the request when the named tool's effective `visibility` (§26.3) does not include `"app"`. The host SHOULD confirm with the user before honoring `ui/open-link` and before inserting a `ui/message` into the conversation.
- **No credential or context leakage.** The host MUST NOT expose credentials, authorization tokens (§23 Authorization), or conversation/context data unrelated to the rendered UI to the UI. Only the tool input and result the UI was rendered for, and the host context explicitly delivered through the dialect, are made available to the UI.
- **Message validation.** The host MUST validate every incoming dialect message against the JSON-RPC framing of §3 Base Message Format before acting on it, and MUST treat the rendered content as untrusted.

### §26.8 Error Handling

Requests in the dialect that fail are answered with a JSON-RPC error response per §3 Base Message Format and §22 Error Handling and Error Codes. A host that declines a UI-initiated `tools/call`, `resources/read`, `ui/open-link`, `ui/message`, or `ui/update-model-context` — whether for lack of consent, policy, or unknown method — MUST return an error response with an appropriate code from §22 Error Handling and Error Codes rather than silently dropping the request. A receiver that receives a dialect method it does not implement MUST respond with the method-not-found error defined in §22 Error Handling and Error Codes.

### §26.9 SDK Scope Summary

The following are server-SDK concerns and MUST be supported by a server-side implementation of this extension:

- Acknowledging the `io.modelcontextprotocol/ui` extension in the `server/discover` result when the host advertises it (§26.2).
- Declaring the UI association on a tool through `_meta.ui` with `resourceUri` and OPTIONAL `visibility` (§26.3).
- Serving the UI resource through the standard `resources/read` mechanism at its `ui://` URI with the MIME type `text/html;profile=mcp-app`, optionally carrying `_meta.ui` presentation and security hints (§26.4, §17 Resources).

The following are host/client concerns and are NOT obligations of a server SDK:

- Rendering the UI in a sandboxed, isolated browsing context and enforcing CSP and permissions (§26.4, §26.7).
- Running the message-channel dialect runtime: the initialization handshake, tool input/result delivery, mediation of UI-initiated requests, lifecycle notifications, and teardown (§26.5).
- Obtaining user consent and applying policy for every action the UI requests (§26.7).

# Part VIII — Governance, Security, and Reference

## §27 Feature Lifecycle and Deprecation

This section defines the lifecycle states a protocol feature occupies, the policy that governs movement between those states, and the registry of features that this document defines but discourages for new use. A "feature," for the purposes of this section, is any protocol message, capability, transport, schema type, metadata key, or normative behavioral requirement defined elsewhere in this document. [SEP-2596]

### §27.1 Feature Lifecycle State Model

Every feature governed by this document is, at any moment, in exactly one of three lifecycle states:

- **Active.** The feature is fully supported and recommended. Implementations MUST implement Active features in accordance with the normative requirements stated where the feature is defined, subject to the capability negotiation rules of §6 Capabilities and Extensions.
- **Deprecated.** The feature remains defined and functional but is discouraged for new use. A Deprecated feature is scheduled for eventual removal and is accompanied by a migration note (see §27.3 Registry of Deprecated Features). Implementations MUST continue to honor a Deprecated feature exactly as specified for as long as it remains defined.
- **Removed.** The feature is not defined by this document. A Removed feature carries no normative meaning here and imposes no implementation obligation.

This document defines only features in the Active and Deprecated states. Removed features are simply absent: they appear neither in the normative text, the schema, nor any registry of this document. An implementation MUST NOT infer the meaning of a name, code, key, or method that this document does not define, and MUST treat such an absence in accordance with the forward-compatibility rules of §6 Capabilities and Extensions and the error-handling rules of §22 Error Handling and Error Codes. [SEP-2596]

The lifecycle state of an individual feature is independent of the protocol revision string, whose negotiation is governed by §5 Protocol Revision, Version Negotiation, and Discovery. The single revision string carried on the wire is the literal value `2026-07-28`.

### §27.2 Deprecation Policy

The transition of a feature from Active toward absence is constrained as follows:

1. **Deprecation precedes removal.** A feature MUST pass through the Deprecated state before it becomes Removed. A feature MUST NOT transition directly from Active to Removed. [SEP-2596]
2. **Minimum deprecation window and earliest removal.** A feature MUST remain in the Deprecated state for a minimum window of twelve (12) months before it is eligible for removal. The window is measured from the point at which the feature first becomes Deprecated. A feature becomes eligible for removal in the first protocol revision released as Current on or after that window elapses; that point is the feature's **earliest removal**. The earliest removal marks only when a feature becomes eligible for removal; it does not compel removal, and a feature MAY remain Deprecated for substantially longer than this minimum. [SEP-2596]
3. **Continued function during the window.** While a feature is Deprecated, it MUST remain functional and MUST behave exactly as specified where it is defined. An implementation MUST NOT degrade, disable, or alter the observable semantics of a feature solely because it is Deprecated. [SEP-2596]
4. **Migration path.** A Deprecated feature MUST be accompanied by a documented migration path or by an explicit statement that none is required. Where the migration path names a replacement, that replacement MUST itself be an Active feature of this document. [SEP-2596]
5. **Discouragement of new adoption.** Implementations SHOULD NOT adopt a Deprecated feature for new functionality and SHOULD migrate existing functionality away from Deprecated features toward their documented replacements before the feature's eligibility for removal. [SEP-2577]
6. **Expedited removal on active security risk.** The twelve-month minimum window MAY be shortened only when the Deprecated feature presents an active security risk — a vulnerability with a published security advisory or documented in-the-wild exploitation for which no in-place mitigation exists. A shortened window MUST still provide at least ninety (90) days between the point at which the feature becomes Deprecated and its earliest removal; a window shorter than ninety days is NOT permitted. While the feature remains Deprecated under a shortened window, items 3 and 4 of this section continue to apply: the feature MUST remain functional and behave exactly as specified, and its documented migration path MUST remain in force. [SEP-2596]
7. **Restoration to Active.** A Deprecated feature MAY be restored to the Active state. When a feature is restored, it again becomes fully supported and is not scheduled for removal, and implementations MUST treat it as an Active feature under §27.1. If a restored feature is subsequently deprecated again, the minimum deprecation window of item 2 is measured afresh from the point at which the new deprecation first takes effect; time already spent Deprecated MUST NOT be counted toward the new window. [SEP-2596]

The keywords MUST, MUST NOT, SHOULD, SHOULD NOT, and MAY in this section are to be interpreted as described in §2 Conformance, Terminology, and Notation. [RFC2119][RFC8174]

### §27.3 Registry of Deprecated Features

The following features are defined by this document and are in the Deprecated state. Each entry gives a one-line migration note and a cross-reference to the section where the feature is defined. New implementations SHOULD NOT adopt these features, and existing implementations SHOULD migrate as indicated. Each entry additionally states the feature's earliest removal — the first revision released as Current on or after its minimum deprecation window elapses — before which existing implementations SHOULD migrate. [SEP-2577][SEP-2596]

- **Roots capability** — defined in §21 Deprecated Client-Provided Capabilities. *Migration:* convey directory or file locations through tool parameters, resource URIs, or out-of-band server configuration rather than through the Roots capability. [SEP-2577]
- **Sampling capability** — defined in §21 Deprecated Client-Provided Capabilities. *Migration:* integrate directly with a language-model provider interface rather than requesting model completions from the client through the protocol. [SEP-2577]
- **The `includeContext` values `"thisServer"` and `"allServers"`** (a field of the Sampling capability) — defined in §21 Deprecated Client-Provided Capabilities. *Migration:* omit the field or use the value `"none"`. The two named values are case-sensitive. [SEP-2596]
- **Logging capability** — defined in §15 Utilities: Progress, Cancellation, Logging, and Trace Context. *Migration:* for the stdio transport (§8 The stdio Transport), write diagnostic output to the standard error stream; for general observability, emit telemetry through an external observability framework. [SEP-2577]
- **The `io.modelcontextprotocol/logLevel` metadata key** (associated with the Logging capability) — defined in §15 Utilities: Progress, Cancellation, Logging, and Trace Context; recorded in Appendix C Reserved _meta Key Registry. *Migration:* follows the Logging capability; do not introduce this key for new functionality. The key string is case-sensitive. [SEP-2577]
- **Dynamic Client Registration**, as a client-registration mechanism — defined in §23 Authorization. *Migration:* use the client-identity registration mechanism described in §23 Authorization in place of dynamic registration. [SEP-2596]

This registry is a derived, consolidated view; the per-feature deprecation notices at the sections cross-referenced above are the authoritative records, and any apparent conflict is resolved in favor of the defining section.

### §27.4 Signaling and Handling Deprecation

Implementations signal and accommodate deprecation as follows:

- **Native deprecation marking.** Where an implementation exposes a Deprecated feature through an API surface that has a native deprecation mechanism in its language (for example `@Deprecated` in Java, `[Obsolete]` in .NET, the `@deprecated` JSDoc tag in TypeScript, or the `Deprecated:` documentation convention in Go), the implementation MUST mark that surface deprecated using that mechanism, and SHOULD reference the migration path of §27.3 and the feature's earliest removal (§27.2) where the mechanism permits. An implementation SHOULD also mark the feature as deprecated in its own documentation, identifying the migration path defined in §27.3 Registry of Deprecated Features. [SEP-2596]
- **Runtime signaling.** An implementation that exercises or accepts a Deprecated feature SHOULD emit a runtime warning through a mechanism idiomatic to its environment (for example, a logged warning or a diagnostic notification). Such a warning MUST NOT be emitted on the protocol wire in a manner that alters the message format defined in §3 Base Message Format or the semantics of any response; warnings are advisory and out of band with respect to normative message processing.
- **Continued interoperability.** A peer MUST continue to interoperate with a peer that still uses a Deprecated feature for as long as that feature remains defined by the negotiated protocol revision. A peer MUST NOT reject, fault, or terminate an otherwise valid exchange solely because the other peer relies on a Deprecated feature, and MUST NOT return an error under §22 Error Handling and Error Codes for that reason alone. Capability negotiation for Deprecated features follows the ordinary rules of §6 Capabilities and Extensions.
- **No reliance on signaling.** The absence of a deprecation warning MUST NOT be interpreted as an indication that a feature is Active; a peer determines lifecycle state from this document and §27.3 Registry of Deprecated Features, not from the presence or absence of runtime signals.

### §27.5 Lifecycle of Extensions

Extensions, defined through the mechanism of §24 The Extension Mechanism and carried in the extensions map of §6 Capabilities and Extensions, are not features of this document and therefore carry their own independent lifecycle. The states, deprecation policy, minimum window, and signaling obligations of this section govern only the features defined by this document; they do not constrain when or how an extension is introduced, deprecated, or removed by its own governing definition. An implementation MUST determine the lifecycle state of an extension from that extension's own definition, and MUST NOT assume that the deprecation window or removal rules of §27.2 Deprecation Policy apply to it. The negotiation and forward-compatibility handling of extensions, including unknown extensions, follow §6 Capabilities and Extensions and §24 The Extension Mechanism.

## §28 Security Considerations

This section consolidates the normative security requirements that apply across the whole protocol. It is not a substitute for the security-relevant requirements stated alongside individual features; rather, it states the cross-cutting principles, restates the most critical obligations, and cross-references the sections in which feature-specific rules are defined. The Model Context Protocol enables powerful capabilities through arbitrary data access and code-execution paths, and with that power come security and trust obligations that every implementation MUST address [MCP]. The protocol cannot enforce most of these obligations at the wire level; conformance therefore depends on implementations honoring the requirements stated here.

### §28.1 Core Security Principles

Every conforming implementation MUST be designed around the following four principles. They are the foundation from which the more specific requirements in this section derive.

1. **User consent and control.** Users MUST explicitly consent to, and be able to understand, all data access and operations. Users MUST retain control over what data is shared and what actions are taken, and implementations SHOULD provide clear interfaces for reviewing and authorizing activities [MCP].
2. **Data privacy.** A server receives only the context the host chooses to share. Hosts MUST obtain explicit user consent before exposing user data to a server, and MUST NOT transmit resource data elsewhere without user consent. User data SHOULD be protected with appropriate access controls [MCP].
3. **Tool safety.** Tools represent arbitrary code execution and MUST be treated with appropriate caution. Descriptions of tool behavior, including annotations, MUST be considered untrusted unless obtained from a trusted server. Hosts MUST obtain explicit user consent before invoking any tool [MCP].
4. **Host-mediated trust.** The host is the trust boundary between the user and the set of connected servers (§1 Introduction). The host mediates every flow of data and authority between the user, the model, and a server. Trust decisions — which servers to connect, which context to expose, which tools to permit — are made and enforced at the host, not delegated to a server.

Because MCP itself cannot enforce these principles at the protocol level, implementors SHOULD build robust consent and authorization flows into their applications, provide clear documentation of security implications, implement appropriate access controls and data protections, follow security best practices in their integrations, and consider privacy implications in their feature designs [MCP].

### §28.2 User Consent and Control

The host is the locus of consent. A host MUST obtain explicit user consent before exposing any user data to a server and before invoking any tool (§16 Tools), elicitation (§20 Elicitation), or other operation that acts on the user's behalf. Consent MUST be informed: the user MUST be given enough information to understand what data will be shared or what action will be taken before authorizing it.

Users MUST be able to review and authorize activities and MUST be able to decline them. A host MUST NOT treat the absence of an explicit refusal as consent, and MUST NOT silently escalate an already-granted consent to cover a broader scope of data or a different operation. Where an operation differs materially from one the user already authorized, the host MUST seek fresh consent. Hosts SHOULD present consent prompts in a form that cannot be spoofed by server-provided content (see §28.8).

### §28.3 Tool Safety

A tool invocation is, in general, a request to execute arbitrary code with effects the host cannot predict from the request alone. Implementations MUST treat tools accordingly.

- Tool definitions — including names, descriptions, input and output schemas, and annotations (§16 Tools) — MUST be treated as untrusted unless they were obtained from a server the host trusts. A receiver MUST NOT rely on a tool annotation (for example, a hint that a tool is read-only or non-destructive) as a security guarantee; such metadata is descriptive, not authoritative, and a malicious or compromised server may misstate it.
- The host MUST keep a human in the loop for tool execution: a user MUST be able to review a proposed tool invocation, understand what it does, and deny it before it runs [MCP]. The decision to invoke a tool MUST NOT rest solely with the model.
- Hosts SHOULD guard against prompt-injection content reaching the model through tool descriptions, tool results, or resource contents, since such content can attempt to induce the model to request actions the user did not intend. The human-in-the-loop requirement is the backstop that prevents such induced requests from executing without review.
- A server that exposes tools MUST rate-limit `tools/call` invocations so that a hostile or malfunctioning client cannot drive unbounded execution or downstream load; requests exceeding the limit MUST be rejected rather than executed [MCP].
- A server MUST sanitize tool outputs before returning them, so that a tool result cannot carry control sequences, markup, or injected instructions that would compromise the client, the model, or downstream consumers (§16 Tools) [MCP].
- Before issuing a `tools/call`, a client SHOULD show the tool's arguments to the user, so that the user can detect and prevent malicious or accidental data exfiltration through tool parameters (§16 Tools) [MCP].
- A client SHOULD apply a timeout to each `tools/call` and surface a failure to the user when the timeout elapses, rather than waiting indefinitely on an unresponsive server [MCP].
- A client SHOULD log tool usage for audit purposes, while observing the logging-confidentiality requirements of §28.9 (credentials and tokens MUST NOT be logged) [MCP].

### §28.4 Data Privacy and Isolation

A server MUST receive only the context the host elects to share with it. Hosts MUST NOT transmit resource data (§17 Resources) or other user data to a server, or onward to any third party, without the user's consent. User data SHOULD be protected with access controls commensurate with its sensitivity [MCP].

Servers MUST be isolated from one another. One server MUST NOT be able to observe the existence of, the data exchanged with, or the activity of another server connected to the same host (§1 Introduction). The host MUST NOT relay one server's requests, results, context, or credentials to another server. This isolation is a primary reason trust is mediated by the host: a server's view of the system is limited to what the host deliberately exposes to it.

### §28.5 Authorization Security

When authorization is used, implementations MUST satisfy the normative requirements of §23 Authorization. This subsection restates the security obligations that are most consequential cross-cutting concerns; §23 is authoritative and prevails in case of any apparent difference.

- **Audience binding and token rejection.** A server MUST validate that every access token presented to it was issued specifically for that server as the intended audience, and MUST reject any token that does not include the server in its audience or that the server cannot otherwise verify was intended for it. A server MUST validate a token before processing the request it accompanies, and MUST NOT return data to an unauthorized party [MCP][RFC8707][RFC9068].
- **No token passthrough / confused deputy.** A server MUST NOT accept a token issued for another resource and MUST NOT forward a token it received from a client onward to an upstream API. When a server itself calls an upstream API, it acts as a client to that API and MUST use a separate token issued by the upstream authorization server. Passing a client-supplied token through to a downstream service creates a confused-deputy vulnerability in which the downstream service may wrongly trust the token as having originated from, or been validated by, the server [MCP].
- **Exact issuer validation (mix-up defense).** A client interacts with many authorization servers over its lifetime; a malicious one may try to have the client redeem a code or token at the wrong endpoint. Before redirecting the user agent, the client MUST record the expected issuer, and on receiving the authorization response it MUST compare any returned issuer identifier against the recorded value using exact string comparison — without scheme or host case folding, default-port elision, trailing-slash, or percent-encoding normalization — and MUST reject mismatches [MCP][RFC9207][RFC3986].
- **Code-interception defense (PKCE).** A client MUST use Proof Key for Code Exchange with the `S256` challenge method where technically capable, and MUST verify (via authorization-server metadata) that the server supports it before proceeding; if support cannot be confirmed, the client MUST refuse to proceed [MCP].
- **Cross-site request forgery defense (`state`).** A client SHOULD generate and verify a `state` value in the authorization code flow, and MUST discard any result whose `state` is absent or does not match the original [MCP].
- **Token confidentiality.** Tokens are sensitive credentials. Clients and servers MUST store tokens securely and MUST keep refresh tokens confidential in transit and at rest. Tokens MUST NOT be logged, and MUST NOT be forwarded to any party other than the one for which they were issued; a token cached or logged on a server is as exploitable as one stolen from a client [MCP]. Authorization-server endpoints and redirect URIs MUST use HTTPS (a `localhost` redirect URI is permitted) [MCP].

### §28.6 Multi-Round-Trip and Continuation Safety

The `requestState` continuation token used to resume a multi-round-trip request (§11 Multi-Round-Trip Requests) is minted by the server and is opaque to the client. Because that token carries server-side continuation state across an otherwise stateless boundary (§4 Request Metadata and the Stateless Model), a server MUST protect both its integrity and its confidentiality so that a client cannot read, forge, or tamper with the continuation state it represents — for example by signing it, encrypting it, or both, or by treating it as an unguessable handle to state held entirely on the server. A receiver MUST reject a continuation token that fails integrity validation rather than acting on its contents. Servers SHOULD guard against replay of continuation tokens, for example by binding a token to a single use or to the session and operation it was issued for, and by bounding its validity in time.

### §28.7 Elicitation and Sampling Consent

Both server-initiated elicitation (§20 Elicitation) and any deprecated client-provided capability that lets a server obtain model output (§21 Deprecated Client-Provided Capabilities) carry the user's data and authority into a server-driven flow, so both MUST remain under user control.

- For elicitation, the client MUST give the user the ability to review the request and to approve, edit, decline, or cancel the response before anything is returned to the server. The user MUST be able to decline or cancel at any point.
- A server MUST NOT use elicitation to phish for credentials or other secrets; clients SHOULD make the requesting server's identity clear in the elicitation interface and SHOULD treat requests for passwords, tokens, or similar secrets as suspect.
- Where a server can drive model sampling, the prompts sent for sampling and the completions produced MUST be subject to human review: the user MUST be able to see and approve what is sent to the model and what is returned before it is acted upon or transmitted to a server. The host MUST NOT disclose more conversation context or data to such a request than the user has authorized.

### §28.8 User-Interface Sandboxing

Where a server provides interactive user-interface content (§26 The Interactive User-Interface Extension), the host MUST render that content in an isolated, sandboxed execution context governed by a restrictive content-security policy, so that server-provided code cannot reach the host's privileges, the user's other data, or other connected servers. The host:

- MUST mediate every privileged action the UI requests. A tool invocation requested by server-provided UI MUST be routed through the host's normal consent and human-in-the-loop path (§28.3); the UI MUST NOT be able to cause a tool to run without the host's mediation and the user's consent.
- MUST NOT expose credentials, tokens, or context unrelated to the UI's purpose to the sandboxed content, and MUST NOT allow the sandboxed content to exfiltrate host or user state through navigation, network access, or inter-frame communication beyond what the policy permits.
- SHOULD constrain the sandbox's network, storage, and scripting capabilities to the minimum the feature requires, and SHOULD ensure that host-rendered consent and identity indicators cannot be spoofed or obscured by the sandboxed content.

### §28.9 Metadata and Observability

Metadata accompanying a request or notification — including trace-context propagation values and other diagnostic fields (§4 Request Metadata and the Stateless Model, §15 Utilities: Progress, Cancellation, Logging, and Trace Context) — is opaque and untrusted. A receiver MUST NOT treat any metadata value as a source of authority: trace identifiers, progress tokens, and similar fields MUST NOT be used for authentication, authorization, or any access-control decision, since a peer can set them to arbitrary values. Receivers SHOULD validate the structure of metadata they consume and ignore values they do not understand.

Implementations SHOULD avoid logging sensitive metadata and SHOULD avoid recording sensitive request or result content in logs, traces, or telemetry. In particular, credentials and tokens MUST NOT be logged (§28.5). Where observability data may transit or be stored outside the host's trust boundary, implementations SHOULD minimize and redact it accordingly.

### §28.10 Input Validation and Resource Bounds

A receiver MUST validate all inputs it accepts from a peer before acting on them, and MUST NOT assume a peer is well-behaved.

- **Argument validation.** A server MUST validate tool-call arguments against the tool's declared input schema, and a client SHOULD validate structured results against a declared output schema, before relying on them (§16 Tools). Validation failures MUST be reported as errors (§22 Error Handling and Error Codes) rather than acted upon.
- **URI validation.** A receiver MUST validate resource URIs and URI templates before dereferencing or matching them (§17 Resources), and MUST NOT follow a URI to a location the user has not authorized. Implementations SHOULD guard against server-side request forgery when a URI could cause the receiver to issue a network request.
- **Origin validation and DNS-rebinding defense.** A server exposing an HTTP endpoint MUST validate the `Origin` header on every incoming connection and reject untrusted origins to defend against DNS-rebinding attacks, as required by §9.11 Security and Endpoint Binding.
- **Cursor validation.** A server MUST treat a pagination cursor as opaque and untrusted input, MUST validate it, and MUST reject a malformed, unknown, or expired cursor with an error rather than interpreting attacker-controlled contents (§12 Pagination).
- **Bounded consumption.** A receiver MUST bound the resources consumed while validating inputs. In particular, implementations MUST bound schema nesting depth and the time spent validating against a schema (§16 Tools) so that a hostile or pathological schema or payload cannot cause unbounded computation or memory use. Receivers SHOULD impose limits on message and payload size and reject inputs that exceed them.
- **No automatic external dereferencing.** A server MUST NOT automatically dereference external schema references encountered in a tool schema (§16 Tools); resolving such references on demand would let a peer drive the server to fetch arbitrary locations and would expose it to server-side request forgery and resource-exhaustion attacks. Schemas MUST be self-contained or resolved only against sources the implementation explicitly trusts.
- **File-path sanitization.** When serving `file://` resources, a server MUST sanitize file paths to prevent directory-traversal attacks (for example, paths containing `..` segments or absolute paths escaping the authorized root), and MUST NOT serve a file outside the directories the user has authorized (§17 Resources) [MCP].

## §29 Conformance Requirements

### §29.1 Meaning of Conformance

An implementation is **conformant** if, and only if, it correctly satisfies every normative requirement (every MUST, MUST NOT, REQUIRED, SHALL, and SHALL NOT, and every applicable SHOULD/SHOULD NOT/MAY as constrained therein) that applies to the role or roles it plays and to the features it advertises. The key words are interpreted as in [RFC2119][RFC8174] and as established in §2 Conformance, Terminology, and Notation.

Conformance is scoped along three independent axes:

1. **Role.** An implementation acts as a **client**, a **server**, or both. A requirement that names a role binds an implementation only when it plays that role. An implementation that plays both roles MUST satisfy the requirements of each.
2. **Feature surface.** Beyond the baseline (§29.2, §29.3), an implementation is bound by a feature's requirements only when it advertises that feature's capability or extension. Advertisement is the contract: see §29.4 and §29.5.
3. **Transport.** An implementation is bound by the requirements of each transport it implements (§29.8).

Every conformant implementation MUST use the message format of §3 Base Message Format for all protocol traffic and MUST operate under the stateless, per-request model of §4 Request Metadata and the Stateless Model. There is no negotiation handshake and no session establishment in the base protocol: each request is self-contained, carries its own metadata, and is accepted or rejected on its own merits [MCP-Versioning]. An implementation that derives any protocol-significant state (protocol revision, peer identity, advertised capabilities) from the identity or prior traffic of a connection, process, or stream — rather than from the per-request envelope of §4 — is NOT conformant.

Conformance is a property of observable behavior on the wire. An implementation MAY satisfy these requirements by any internal architecture, in any programming language; this specification constrains messages and externally visible behavior only (§29.9).

### §29.2 Baseline Server Conformance

Every conformant server, regardless of which features it offers, MUST satisfy all of the following:

1. **Discovery.** A server MUST implement the `server/discover` method (§5 Protocol Revision, Version Negotiation, and Discovery). A client MAY call it before any other request to learn the server's supported protocol revisions and capabilities up front, but is not obligated to; the server's obligation to answer it is unconditional [MCP-Versioning].
2. **Advertisement.** A server MUST advertise the protocol revisions it supports and the capabilities it offers, via the result of `server/discover` (§5) and consistently with §6 Capabilities and Extensions. A server MUST NOT advertise a protocol revision or capability whose required behavior it does not implement (§29.4).
3. **Per-request metadata.** A server MUST honor the per-request metadata envelope of §4 on every request it processes and MUST NOT infer any protocol-significant state across requests, even requests arriving on the same connection, process, or stream (§4). A server MUST NOT require that a client reuse the same connection or process to perform related operations.
4. **Unsupported revision.** When a request declares a protocol revision the server does not implement (whether unknown to the server or a known revision the server elects not to support), the server MUST reject that request with error code `-32004` (the unsupported-protocol-version error of §22 Error Handling and Error Codes), and the error `data` MUST list the revisions the server does support and the revision that was requested [MCP-Versioning]. The catalog of revisions a server supports always includes the wire value `2026-07-28`.
5. **Missing required capability.** When processing a request requires a client capability that the request's metadata does not declare (§4), the server MUST reject the request with error code `-32003` (the missing-required-client-capability error of §22), and the error `data` MUST list the missing required capabilities.
6. **Malformed envelope.** When a request omits any field that §4 marks as required, the request is malformed and the server MUST reject it with error code `-32602` (Invalid params, §22). The corresponding transport-level failure mapping is governed by §29.8 and §9 The Streamable HTTP Transport.
7. **Result typing.** A server MUST set the `resultType` discriminator on every successful result it returns (§3). The value MUST be drawn from the set defined by the core protocol together with any values contributed by extensions the server has advertised (§3, §6).
8. **Capability gating.** A server MUST gate every feature behind its advertised capability (§6). A server MUST NOT expose, exercise, or depend upon any behavior that it has not advertised, and MUST NOT solicit a client behavior the client has not declared (see §29.4 item 5 and §11 Multi-Round-Trip Requests).

### §29.3 Baseline Client Conformance

Every conformant client, regardless of which features it consumes, MUST satisfy all of the following:

1. **Per-request envelope.** A client MUST include, in the per-request metadata of every request it sends (§4), the protocol revision in use, the client identity, and the client capabilities relevant to that request. These fields are REQUIRED on every request; the stateless model forbids the client from relying on the server to remember them from an earlier request (§4).
2. **Revision selection.** A client MUST send a protocol revision it supports, and MUST be able to select a revision mutually supported with the server (§5 Protocol Revision, Version Negotiation, and Discovery). On receiving a `-32004` rejection (§22), a client SHOULD select a revision from the server's advertised `supported` list and retry, or surface an error to the user if no mutually supported revision exists [MCP-Versioning].
3. **Opacity.** A client MUST treat as opaque every value the specification designates opaque, including pagination cursors (§12 Pagination), the `requestState` of a multi-round-trip exchange (§11 Multi-Round-Trip Requests), subscription identifiers (§10 Server-to-Client Streaming and Subscriptions), and any continuation or handle values carried in request metadata (§4). A client MUST NOT inspect, parse, modify, or assume anything about the contents of such values; when the protocol requires echoing one back, the client MUST echo the exact value unchanged [MCP-MRTR].
4. **Input-required fulfillment.** A client MUST be able to fulfill an `input_required` result for the capabilities it declares (§11). When a server returns an `input_required` result carrying input requests (§11), a client MUST construct the requested inputs before retrying the original request; if no input requests are present, the client MAY retry immediately. The retry MUST use a request identifier distinct from the original request, MUST echo back any `requestState` exactly when one was provided, and MUST NOT include a `requestState` when none was provided [MCP-MRTR] (§11, §3).
5. **Result interpretation.** A client MUST interpret each successful response according to its `resultType` discriminator (§3) and MUST apply the robustness rules of §29.6 to discriminator values, fields, and codes it does not recognize.

### §29.4 Capability-Conditioned Conformance

Beyond the baseline, an implementation incurs additional obligations exactly in proportion to what it advertises (§6 Capabilities and Extensions). The governing principle is bidirectional:

1. **Advertise implies implement.** An implementation that advertises a capability MUST implement every MUST-level behavior defined for that capability. In particular:
   - A server advertising tools MUST satisfy the tools requirements of §16 Tools.
   - A server advertising resources MUST satisfy the resources requirements of §17 Resources, and a server advertising resource subscriptions MUST additionally satisfy the subscription behaviors of §10 Server-to-Client Streaming and Subscriptions.
   - A server advertising prompts MUST satisfy the prompts requirements of §18 Prompts.
   - A server advertising completion MUST satisfy the completion requirements of §19 Completion.
   - A client advertising elicitation MUST satisfy the elicitation requirements of §20 Elicitation.
   - Any party advertising a streaming or subscription capability MUST satisfy the applicable requirements of §10.
2. **Implement implies advertise.** An implementation MUST NOT exercise, expose, or depend on a feature it has not advertised. A server MUST NOT return a result type, solicit a client capability, or invoke a behavior outside what it has advertised (§29.2 item 8).
3. **Never advertise the unimplemented.** An implementation MUST NOT advertise a capability whose required behavior it does not implement. Advertising a capability is a binding assertion of full MUST-level conformance for that capability.
4. **No reliance on undeclared peer capabilities.** A server MUST NOT rely on a client capability the client has not declared in the request's metadata (§4); if such a capability is required, the server MUST respond per §29.2 item 5 (`-32003`).
5. **No unsolicited input requests.** A server MUST NOT place into an `input_required` result any input request of a kind the client has not declared support for; for example, a server MUST NOT include an elicitation input request unless the client declared the elicitation capability [MCP-MRTR] (§11, §20).

The deprecated client-provided capabilities of §21 Deprecated Client-Provided Capabilities are subject to the same bidirectional rule: an implementation that advertises one MUST implement its specified behavior, and one that does not advertise it MUST NOT rely on it.

### §29.5 Optionality of Extensions and Deprecated Features

1. **Extensions are OPTIONAL.** The extension mechanism (§24 The Extension Mechanism) and the extensions it defines, including the Tasks extension (§25 The Tasks Extension) and the Interactive User-Interface extension (§26 The Interactive User-Interface Extension), are OPTIONAL. An implementation that advertises zero extensions is fully conformant. Extensions are opt-in and require explicit support from both parties, advertised in the extensions map of §6 [MCP-Spec].
2. **Conditional extension obligations.** An implementation that advertises an extension MUST implement that extension's MUST-level behaviors and MUST follow its declared fallback behavior. Extension identifiers MUST follow the naming rules of §6. When one party advertises an extension the other does not, the supporting party MUST either revert to core protocol behavior or reject the affected request with an appropriate error [MCP-Versioning] (§24, §22).
3. **Deprecated features.** Features whose status is Deprecated under §27 Feature Lifecycle and Deprecation are OPTIONAL to implement. An implementation that does implement a Deprecated feature MUST follow that feature's specified behavior in full; partial or divergent implementation of a Deprecated feature is NOT conformant.

### §29.6 Robustness and Forward Compatibility

A conformant implementation MUST be tolerant of inputs richer than it understands, so that peers supporting additional revisions, capabilities, or extensions can interoperate. Specifically:

1. **Unknown object fields.** An implementation MUST ignore fields it does not recognize in any received object, rather than rejecting the message (§2 Conformance, Terminology, and Notation). This permits forward-compatible field additions.
2. **Unknown capabilities.** An implementation MUST ignore advertised capabilities it does not recognize (§2, §6) and MUST NOT treat their presence as an error.
3. **Unknown extension identifiers.** An implementation MUST ignore extension identifiers it does not recognize in the extensions map (§6, §24); an unrecognized extension is simply unsupported and triggers the fallback of §29.5 item 2.
4. **Unknown error codes.** A client MUST accept and handle error codes it does not specifically recognize (§22 Error Handling and Error Codes), treating them as failures of the request without crashing or misclassifying them.
5. **Unrecognized result type.** A `resultType` value not recognized by the receiver MUST be treated as an error (§3); a client MUST NOT attempt to act on a result whose discriminator it cannot interpret. Where the discriminator is absent on a result, the receiver MUST apply the absence rule defined in §3.

Ignoring an unknown field, capability, or extension MUST NOT cause the implementation to silently discard semantically required content it does understand; robustness applies only to the unrecognized.

### §29.7 Conformance and the Stateless Model

Because the base protocol is stateless (§4 Request Metadata and the Stateless Model), conformance includes the following invariants that bind every role:

1. A server MUST process each request independently and MUST NOT infer context — capabilities, protocol revision, peer identity, prior selections — from any earlier request, including one on the same connection or stream (§4).
2. State that must span multiple requests (for example, a long-running operation, an application-level handle, or a multi-round-trip exchange) MUST be referenced by an explicit identifier or opaque value the client supplies on each request (§4, §11 Multi-Round-Trip Requests).
3. A long-lived request that yields a stream of notifications (§10 Server-to-Client Streaming and Subscriptions) remains a single request/response: its state is scoped to that request, not to the underlying connection. An implementation MUST NOT treat the connection or process as the lifetime boundary of a conversation, task, or subscription (§4).
4. When a `requestState` value passes through a client, a server MUST treat it as attacker-controlled input. If `requestState` influences authorization, resource access, or business logic, the server MUST protect its integrity and MUST reject state that fails verification [MCP-MRTR] (§11, §28 Security Considerations).

### §29.8 Transport Conformance

1. **At least one transport.** A conformant implementation MUST implement at least one transport defined in §7 Transport Model.
2. **Per-transport requirements.** For each transport it implements, an implementation MUST uphold that transport's framing, message routing, and error-mapping requirements: the stdio transport per §8 The stdio Transport, and the Streamable HTTP transport per §9 The Streamable HTTP Transport.
3. **Error mapping.** Protocol-level errors MUST be mapped to transport-level signals as the chosen transport specifies. In particular, on the Streamable HTTP transport, a malformed per-request envelope and a missing required field MUST produce the HTTP status the transport prescribes for an Invalid params (`-32602`) condition, and a missing required client capability (`-32003`) MUST produce the prescribed status, as defined in §9.
4. **Authorization.** An implementation using an HTTP-based transport SHOULD conform to §23 Authorization. An implementation using the stdio transport SHOULD NOT apply that framework and instead obtains credentials from its environment as specified in §8 and §23 [MCP-Spec].
5. **Transport independence.** An implementation MUST NOT make conformance of one transport contingent on another; each transport it offers MUST independently satisfy its own requirements. Multiple transports MAY be offered concurrently.

### §29.9 Determining Conformance

1. **Self-contained sufficiency.** An implementation built to satisfy every applicable requirement of this specification, for the roles it plays and the features it advertises, is conformant; no behavior outside this document is required for interoperation at the protocol level.
2. **Observable basis.** Conformance is assessed against observable wire behavior — the messages exchanged and the transport signals produced — and not against internal structure. Two implementations producing identical observable behavior for identical inputs are equally conformant.
3. **Profile of conformance.** An implementation's conformance is fully described by the tuple of (a) the role or roles it plays, (b) the protocol revisions it advertises (always including the wire value `2026-07-28`), (c) the capabilities and extensions it advertises (§6 Capabilities and Extensions), and (d) the transports it implements (§7 Transport Model). The registries of Appendix B Error Code Registry, Appendix C Reserved _meta Key Registry, and Appendix D Capability Registry enumerate the exact codes, metadata keys, and capability identifiers a conformant implementation MUST use for the features within that profile.
4. **No partial feature conformance.** An implementation either fully satisfies the MUST-level behavior of an advertised feature or MUST NOT advertise it (§29.4). There is no conformant intermediate state in which a feature is advertised but only partially implemented.

## §30 Normative and Informative References

Citation markers used throughout this document are provenance only: they identify the external source from which a concept, format, or term originates. They are never load-bearing. All normative content — every MUST, SHOULD, MAY, and the exact wire formats, names, codes, and keys they govern — is fully specified in the body of this document and does not depend on the contents of any cited work. The two subsections below resolve every marker used in the document.

### §30.1 Normative References

- **[CIMD]** — Client ID Metadata Documents. https://modelcontextprotocol.io/specification
- **[CSP]** — Content Security Policy Level 3. https://www.w3.org/TR/CSP3/
- **[HTML-IFRAME]** — WHATWG HTML Living Standard, the iframe element and sandboxed browsing contexts. https://html.spec.whatwg.org
- **[HTML-SSE]** — WHATWG HTML Living Standard, Server-Sent Events (the text/event-stream format). https://html.spec.whatwg.org
- **[JSONRPC2]** — JSON-RPC 2.0 Specification. https://www.jsonrpc.org/specification
- **[JSONSCHEMA]** — JSON Schema (2020-12): Core and Validation. https://json-schema.org/specification
- **[MCP]** — Model Context Protocol specification. https://modelcontextprotocol.io/specification
- **[MCP-MRTR]** — Model Context Protocol specification, multi-round-trip requests (input-required results). https://modelcontextprotocol.io/specification
- **[MCP-Spec]** — Model Context Protocol specification, base protocol and extensions framework. https://modelcontextprotocol.io/specification
- **[MCP-Versioning]** — Model Context Protocol specification, protocol revision, version negotiation, and discovery. https://modelcontextprotocol.io/specification
- **[OAUTH21]** — The OAuth 2.1 Authorization Framework (IETF OAuth Working Group draft of the OAuth 2.1 specification).
- **[OIDC]** — OpenID Connect Core 1.0 and OpenID Connect Dynamic Client Registration 1.0. https://openid.net/connect/
- **[RFC2119]** — S. Bradner, "Key words for use in RFCs to Indicate Requirement Levels", BCP 14, RFC 2119, March 1997.
- **[RFC3986]** — T. Berners-Lee et al., "Uniform Resource Identifier (URI): Generic Syntax", RFC 3986, January 2005.
- **[RFC4648]** — S. Josefsson, "The Base16, Base32, and Base64 Data Encodings", RFC 4648, October 2006.
- **[RFC6570]** — J. Gregorio et al., "URI Template", RFC 6570, March 2012.
- **[RFC6749]** — D. Hardt, Ed., "The OAuth 2.0 Authorization Framework", RFC 6749, October 2012.
- **[RFC6750]** — M. Jones & D. Hardt, "The OAuth 2.0 Authorization Framework: Bearer Token Usage", RFC 6750, October 2012.
- **[RFC7591]** — J. Richer, Ed. et al., "OAuth 2.0 Dynamic Client Registration Protocol", RFC 7591, July 2015.
- **[RFC7636]** — N. Sakimura, Ed. et al., "Proof Key for Code Exchange by OAuth Public Clients", RFC 7636, September 2015.
- **[RFC8174]** — B. Leiba, "Ambiguity of Uppercase vs Lowercase in RFC 2119 Key Words", BCP 14, RFC 8174, May 2017.
- **[RFC8259]** — T. Bray, Ed., "The JavaScript Object Notation (JSON) Data Interchange Format", RFC 8259, December 2017.
- **[RFC8414]** — M. Jones et al., "OAuth 2.0 Authorization Server Metadata", RFC 8414, June 2018.
- **[RFC8707]** — B. Campbell et al., "Resource Indicators for OAuth 2.0", RFC 8707, February 2020.
- **[RFC9068]** — V. Bertocci, "JSON Web Token (JWT) Profile for OAuth 2.0 Access Tokens", RFC 9068, October 2021.
- **[RFC9110]** — R. Fielding et al., "HTTP Semantics", RFC 9110, June 2022.
- **[RFC9207]** — K. Meyer zu Selhausen & D. Fett, "OAuth 2.0 Authorization Server Issuer Identification", RFC 9207, March 2022.
- **[RFC9728]** — M. Jones et al., "OAuth 2.0 Protected Resource Metadata", RFC 9728.
- **[SEP-414]** — Trace-context propagation conventions in request metadata. https://github.com/modelcontextprotocol/modelcontextprotocol (PR 414)
- **[SEP-837]** — Application type in client registration. https://github.com/modelcontextprotocol/modelcontextprotocol (PR 837)
- **[SEP-1865]** — The interactive user-interface extension. https://github.com/modelcontextprotocol/modelcontextprotocol (PR 1865)
- **[SEP-2106]** — JSON Schema 2020-12 for tool input and output schemas. https://github.com/modelcontextprotocol/modelcontextprotocol (PR 2106)
- **[SEP-2133]** — The extensions framework. https://github.com/modelcontextprotocol/modelcontextprotocol (PR 2133)
- **[SEP-2164]** — Resource-not-found signaled with Invalid params. https://github.com/modelcontextprotocol/modelcontextprotocol (PR 2164)
- **[SEP-2207]** — Refresh-token handling. https://github.com/modelcontextprotocol/modelcontextprotocol (PR 2207)
- **[SEP-2243]** — Streamable HTTP routing headers (Mcp-Method, Mcp-Name, parameter headers). https://github.com/modelcontextprotocol/modelcontextprotocol (PR 2243)
- **[SEP-2260]** — Server-to-client interaction during the processing of a client request. https://github.com/modelcontextprotocol/modelcontextprotocol (PR 2260)
- **[SEP-2322]** — Multi-round-trip requests (input-required results). https://github.com/modelcontextprotocol/modelcontextprotocol (PR 2322)
- **[SEP-2350]** — Scope accumulation during step-up authorization. https://github.com/modelcontextprotocol/modelcontextprotocol (PR 2350)
- **[SEP-2351]** — Authorization metadata discovery well-known suffix rules. https://github.com/modelcontextprotocol/modelcontextprotocol (PR 2351)
- **[SEP-2352]** — Binding of client credentials to the authorization-server issuer. https://github.com/modelcontextprotocol/modelcontextprotocol (PR 2352)
- **[SEP-2468]** — Authorization-server issuer identification validation. https://github.com/modelcontextprotocol/modelcontextprotocol (PR 2468)
- **[SEP-2549]** — Response caching hints (ttlMs and cacheScope). https://github.com/modelcontextprotocol/modelcontextprotocol (PR 2549)
- **[SEP-2567]** — Removal of protocol-level sessions from the Streamable HTTP transport. https://github.com/modelcontextprotocol/modelcontextprotocol (PR 2567)
- **[SEP-2575]** — Stateless protocol, per-request metadata envelope, server/discover, and the subscriptions/listen stream. https://github.com/modelcontextprotocol/modelcontextprotocol (PR 2575)
- **[SEP-2577]** — Deprecation of the Roots, Sampling, and Logging features. https://github.com/modelcontextprotocol/modelcontextprotocol (PR 2577)
- **[SEP-2596]** — Feature lifecycle and deprecation policy. https://github.com/modelcontextprotocol/modelcontextprotocol (PR 2596)
- **[SEP-2663]** — The Tasks extension. https://github.com/modelcontextprotocol/modelcontextprotocol (PR 2663)
- **[W3C-BAGGAGE]** — W3C, "Baggage". https://www.w3.org/TR/baggage/
- **[W3C-TRACE]** — W3C, "Trace Context". https://www.w3.org/TR/trace-context/

### §30.2 Informative References

- **[OTEL]** — OpenTelemetry Semantic Conventions. https://opentelemetry.io

## Appendix A. Method and Notification Index

This index lists every JSON-RPC method and notification defined by this document and by its defined extensions (§24 The Extension Mechanism). The **Kind** column states whether the name is a request (expects a response) or a notification (no response). The **Direction** column states the normal sender and receiver; `UI↔host` denotes the user-interface dialect of §26 The Interactive User-Interface Extension, in which the rendered UI and the host exchange messages over the UI message channel. The **Defined in** column cites the section that normatively defines the message.

| Method / Notification | Kind | Direction | Defined in |
| --- | --- | --- | --- |
| `server/discover` | request | client→server | §5 Protocol Revision, Version Negotiation, and Discovery |
| `tools/list` | request | client→server | §16 Tools |
| `tools/call` | request | client→server | §16 Tools |
| `resources/list` | request | client→server | §17 Resources |
| `resources/read` | request | client→server | §17 Resources |
| `resources/templates/list` | request | client→server | §17 Resources |
| `prompts/list` | request | client→server | §18 Prompts |
| `prompts/get` | request | client→server | §18 Prompts |
| `completion/complete` | request | client→server | §19 Completion |
| `subscriptions/listen` | request | client→server | §10 Server-to-Client Streaming and Subscriptions |
| `elicitation/create` | input-request kind † | server→client (via input-required result, §11) | §20 Elicitation |
| `sampling/createMessage` | input-request kind † | server→client (via input-required result, §11) | §21 Deprecated Client-Provided Capabilities |
| `roots/list` | input-request kind † | server→client (via input-required result, §11) | §21 Deprecated Client-Provided Capabilities |
| `tasks/get` | request | client→server | §25 The Tasks Extension |
| `tasks/update` | request | client→server | §25 The Tasks Extension |
| `tasks/cancel` | request | client→server | §25 The Tasks Extension |
| `ui/initialize` | request | UI↔host (UI→host) | §26 The Interactive User-Interface Extension |
| `ui/notifications/initialized` | notification | UI↔host (UI→host) | §26 The Interactive User-Interface Extension |
| `notifications/progress` | notification | client→server or server→client | §15 Utilities: Progress, Cancellation, Logging, and Trace Context |
| `notifications/cancelled` | notification | client→server or server→client | §15 Utilities: Progress, Cancellation, Logging, and Trace Context |
| `notifications/message` | notification | server→client | §15 Utilities: Progress, Cancellation, Logging, and Trace Context |
| `notifications/tools/list_changed` | notification | server→client | §16 Tools |
| `notifications/prompts/list_changed` | notification | server→client | §18 Prompts |
| `notifications/resources/list_changed` | notification | server→client | §17 Resources |
| `notifications/resources/updated` | notification | server→client | §17 Resources |
| `notifications/subscriptions/acknowledged` | notification | server→client | §10 Server-to-Client Streaming and Subscriptions |
| `notifications/elicitation/complete` | notification | server→client | §20 Elicitation |
| `notifications/tasks` | notification | server→client | §25 The Tasks Extension |

† Delivered as an input request embedded inside an input-required result and resolved by client retry (§11 Multi-Round-Trip Requests); NOT a standalone server-initiated JSON-RPC request. See §9.1, §20, and §21.

The user-interface dialect of §26 The Interactive User-Interface Extension defines additional method and notification names exchanged on the UI message channel (`UI↔host`), beyond the two handshake names listed above. These include the host-to-UI tool-data notifications `ui/notifications/tool-input`, `ui/notifications/tool-input-partial`, `ui/notifications/tool-result`, and `ui/notifications/tool-cancelled`; the UI-to-host requests `tools/call`, `resources/read`, `ui/open-link`, `ui/message`, `ui/request-display-mode`, and `ui/update-model-context`; the UI-to-host notification `notifications/message`; the bidirectional `ping` request; the host-to-UI notifications `ui/notifications/size-changed` and `ui/notifications/host-context-changed`; the host-to-UI request `ui/resource-teardown`; and the sandbox-bridging notifications `ui/notifications/sandbox-proxy-ready` (sandbox→host) and `ui/notifications/sandbox-resource-ready` (host→sandbox). All of these are defined in §26 The Interactive User-Interface Extension and are in scope only when that extension is active.

## Appendix B. Error Code Registry

This registry lists every JSON-RPC error `code` value defined by this document, together with its symbolic name and meaning. Codes `-32700`, `-32600`, `-32601`, `-32602`, and `-32603` are the standard JSON-RPC codes [JSONRPC2]. The range `-32000` to `-32099` is the implementation-defined server-error range reserved by JSON-RPC; the `-32001` (`HeaderMismatch`) code lies within it. An implementation defining additional codes MUST NOT collide with any code in this registry. The **Defined in** column cites the section that normatively specifies the code's shape and conditions.

| Code | Name | Meaning | Defined in |
| --- | --- | --- | --- |
| `-32700` | Parse error | Invalid JSON was received; the receiver could not parse the byte stream of the message as JSON text. | §22 Error Handling and Error Codes |
| `-32600` | Invalid Request | The payload is valid JSON but is not a valid JSON-RPC request object (for example, a missing or wrongly typed `jsonrpc` or `method` member). | §22 Error Handling and Error Codes |
| `-32601` | Method not found | The requested method is not implemented or not available (for example, a feature method whose gating capability was not advertised). | §22 Error Handling and Error Codes |
| `-32602` | Invalid params | The method parameters are invalid: a required `_meta` field or method parameter is missing or wrongly typed, an unknown target name was supplied, or required arguments are absent. | §22 Error Handling and Error Codes |
| `-32603` | Internal error | An internal error occurred in the receiver while processing an otherwise valid request. | §22 Error Handling and Error Codes |
| `-32003` | MissingRequiredClientCapability | Fulfilling the request would require a client-provided capability that the request did not declare in `io.modelcontextprotocol/clientCapabilities`; `error.data.requiredCapabilities` lists the missing capabilities. On HTTP transports, returned with status `400 Bad Request`. | §5 Protocol Revision, Version Negotiation, and Discovery |
| `-32004` | UnsupportedProtocolVersion | The request declared a protocol revision in `io.modelcontextprotocol/protocolVersion` that the server does not implement; `error.data.supported` lists the server's supported revisions. On HTTP transports, returned with status `400 Bad Request`. | §5 Protocol Revision, Version Negotiation, and Discovery |
| `-32001` | HeaderMismatch | A Streamable HTTP request was rejected because a routing-header value does not match the corresponding request-body value, or a required routing header is missing or malformed. Returned with HTTP status `400 Bad Request`. Lies within the reserved server-error range `-32000` to `-32099`. | §9 The Streamable HTTP Transport |
| `-32000` to `-32099` | (reserved server-error range) | Implementation-defined server-error range reserved by JSON-RPC [JSONRPC2]. The `-32001` (`HeaderMismatch`) code occupies one value of this range; implementations MAY define additional codes within it provided they do not collide with codes defined by this document. | §22 Error Handling and Error Codes |

## Appendix C. Reserved _meta Key Registry

This registry lists the reserved keys that MAY appear in the `_meta` object (§4 Request Metadata and the Stateless Model). Keys beginning with the `io.modelcontextprotocol/` prefix are reserved by this document; the bare keys `progressToken`, `traceparent`, `tracestate`, and `baggage` are reserved by exception (`progressToken` for out-of-band progress correlation; the other three for distributed-trace-context propagation). The naming and reservation rules for `_meta` keys are defined in §4 Request Metadata and the Stateless Model. The **Used on** column states where each key normally appears. The **Defined in** column cites the section that normatively specifies the key.

| Key | Used on | Meaning | Defined in |
| --- | --- | --- | --- |
| `io.modelcontextprotocol/protocolVersion` | every client request (`_meta`) | The protocol revision the request uses (the wire value, for example `"2026-07-28"`). REQUIRED on client requests. | §4 Request Metadata and the Stateless Model |
| `io.modelcontextprotocol/clientInfo` | every client request (`_meta`) | An `Implementation` object identifying the client software issuing the request. REQUIRED on client requests. | §4 Request Metadata and the Stateless Model |
| `io.modelcontextprotocol/clientCapabilities` | every client request (`_meta`) | A `ClientCapabilities` object declaring, for this specific request, the optional capabilities the client supports. REQUIRED on client requests. | §4 Request Metadata and the Stateless Model |
| `io.modelcontextprotocol/logLevel` | client request `_meta` (OPTIONAL) | The minimum log severity the server may emit while processing this request, as a `LoggingLevel` string. Status: **Deprecated**. | §4 Request Metadata and the Stateless Model |
| `progressToken` | request `_meta` (OPTIONAL) | Out-of-band progress correlation token; the value (a `string` or `number`) is echoed in `notifications/progress` to correlate updates with the originating request. | §15 Utilities: Progress, Cancellation, Logging, and Trace Context |
| `io.modelcontextprotocol/subscriptionId` | notification `_meta` on a subscription stream | Correlates a notification delivered on a `subscriptions/listen` stream with the subscription it belongs to; value is the subscription identifier as a string. | §10 Server-to-Client Streaming and Subscriptions |
| `traceparent` | request and notification `_meta` (OPTIONAL) | W3C Trace Context `traceparent` value, carried unchanged for distributed-trace propagation. | §15 Utilities: Progress, Cancellation, Logging, and Trace Context |
| `tracestate` | request and notification `_meta` (OPTIONAL) | W3C Trace Context `tracestate` value, carried unchanged for distributed-trace propagation. | §15 Utilities: Progress, Cancellation, Logging, and Trace Context |
| `baggage` | request and notification `_meta` (OPTIONAL) | W3C Baggage value, carried unchanged for distributed-trace propagation. | §15 Utilities: Progress, Cancellation, Logging, and Trace Context |
| `io.modelcontextprotocol/tasks` | `extensions` map within client `clientCapabilities` and within server capabilities | Extension identifier declaring support for the Tasks extension; its value is an OPTIONAL settings object (empty `{}` defined). | §25 The Tasks Extension |
| `io.modelcontextprotocol/ui` | `extensions` map within host/server capabilities | Extension identifier declaring support for the Interactive User-Interface extension; the host's value carries the REQUIRED `mimeTypes` array. | §26 The Interactive User-Interface Extension |
| `ui` (the `_meta.ui` key on a tool) | a `Tool` object's `_meta` (§16 Tools) | Declares the user interface associated with a tool: an object with REQUIRED `resourceUri` (a `ui://` URI) and OPTIONAL `visibility`. In scope only when the user-interface extension is active. | §26 The Interactive User-Interface Extension |

Extension-defined identifiers and keys beyond those listed above MAY appear in `_meta` and in the `extensions` capability map; the rules governing extension identifiers and the namespacing of any keys they introduce are defined in §24 The Extension Mechanism, and the prefixing rules for `_meta` keys are defined in §4 Request Metadata and the Stateless Model.

## Appendix D. Capability Registry

This registry lists the capabilities defined by this document. Client capabilities appear in the `io.modelcontextprotocol/clientCapabilities` object carried per request (§4 Request Metadata and the Stateless Model); server capabilities appear in the capabilities advertised through discovery (§5 Protocol Revision, Version Negotiation, and Discovery). Both sides carry an `extensions` map; the extension capabilities listed at the end are negotiated through that map (§6 Capabilities and Extensions, §24 The Extension Mechanism). The **Sub-flags** column lists nested members defined for the capability. The **Defined in** column cites the section that normatively specifies the capability.

| Capability | Side | Sub-flags | Defined in |
| --- | --- | --- | --- |
| `elicitation` | client | `form` (OPTIONAL); `url` mode is the other defined mode (see §20 Elicitation) | §6 Capabilities and Extensions |
| `roots` | client | none (value is `{}`); Status: **Deprecated** | §6 Capabilities and Extensions |
| `sampling` | client | `tools` (OPTIONAL, enables sampling `tools`/`toolChoice` parameters); `context` (OPTIONAL, Deprecated, enables `includeContext` non-`none` values); Status: **Deprecated** | §6 Capabilities and Extensions |
| `extensions` | client | (object map keyed by extension identifier) | §6 Capabilities and Extensions |
| `tools` | server | `listChanged` (OPTIONAL, boolean) | §6 Capabilities and Extensions |
| `resources` | server | `listChanged` (OPTIONAL, boolean); `subscribe` (OPTIONAL, boolean) | §6 Capabilities and Extensions |
| `prompts` | server | `listChanged` (OPTIONAL, boolean) | §6 Capabilities and Extensions |
| `completions` | server | none (value is `{}`) | §6 Capabilities and Extensions |
| `logging` | server | none (value is `{}`); Status: **Deprecated** | §6 Capabilities and Extensions |
| `extensions` | server | (object map keyed by extension identifier) | §6 Capabilities and Extensions |
| `io.modelcontextprotocol/tasks` (Tasks extension) | client and server (via `extensions`) | none (settings value is `{}`) | §25 The Tasks Extension |
| `io.modelcontextprotocol/ui` (Interactive User-Interface extension) | host/client and server (via `extensions`) | host value: `mimeTypes` (REQUIRED, string array, MUST include `"text/html;profile=mcp-app"`); server acknowledgement value MAY be empty | §26 The Interactive User-Interface Extension |

## Appendix E. Consolidated Type Index

This index lists every wire type (interface or type alias) declared in this document, the section that contains its defining declaration, and a one-line statement of purpose. Where a type is restated in more than one section for local convenience, the section listed is the one that contains its full canonical declaration. Types are sorted alphabetically (case-insensitive, ASCII).

| Type | Defined in | One-line purpose |
|------|------------|------------------|
| `Annotations` | §14.6 Annotations | Optional client-facing hints (audience, priority, timestamps) attachable to content and resources. |
| `AudioContent` | §14.4.3 AudioContent | Content block carrying base64-encoded audio data with a MIME type. |
| `AuthorizationServerMetadata` | §23.3 Authorization Server Metadata Discovery | OAuth authorization-server metadata document advertising endpoints and supported capabilities. |
| `BaseMetadata` | §14.1 BaseMetadata: name and title | Common base carrying the programmatic `name` and human-facing `title`. |
| `BlobResourceContents` | §14.5 ResourceContents and variants | Resource contents variant carrying base64-encoded binary data. |
| `BooleanSchema` | §20.4 The restricted form schema | Primitive form-field schema describing a boolean input. |
| `CacheableResult` | §13.1 The CacheableResult Structure | Result mixin carrying caching hints (`ttlMs`, `cacheScope`). |
| `CallToolRequest` | §16.5 Calling tools: `tools/call` | Request to invoke a tool by name with arguments. |
| `CallToolResult` | §16.5 Calling tools: `tools/call` | Successful tool-invocation result carrying content blocks and optional structured output. |
| `CancelledNotification` | §15.2.1 The notifications/cancelled notification | Notification that the sender is cancelling a request the sender issued earlier. |
| `CancelledNotificationParams` | §15.2.1 The notifications/cancelled notification | Parameters of the cancellation notification (target request id and optional reason). |
| `CancelledTask` | §25.4 Task and DetailedTask Object Types | `DetailedTask` variant for a task in the `cancelled` terminal state. |
| `CancelTaskRequest` | §25.9 Cancelling a Task: tasks/cancel | Request to cancel an in-progress task by `taskId`. |
| `CancelTaskResult` | §25.9 Cancelling a Task: tasks/cancel | Empty acknowledgement returned for a task cancellation. |
| `ClientCapabilities` | §6.2 ClientCapabilities | Capability set a client advertises to the server. |
| `ClientIdMetadataDocument` | §23.12 Client ID Metadata Documents | Client-published metadata document identified by a client-id URL. |
| `ClientRegistrationRequest` | §23.14 Dynamic Client Registration | Dynamic client registration request body. |
| `ClientRegistrationResponse` | §23.14 Dynamic Client Registration | Dynamic client registration response carrying issued client credentials. |
| `ClientSamplingCapability` | §21.2.3 Client Capability | Client capability declaring support for the deprecated sampling input-request kind. |
| `CompletedTask` | §25.4 Task and DetailedTask Object Types | `DetailedTask` variant for a task in the `completed` terminal state. |
| `CompleteRequest` | §19.2 `completion/complete` request | Request for completion suggestions for a prompt or resource-template argument. |
| `CompleteRequestParams` | §19.2 `completion/complete` request | Parameters of a completion request (reference, argument, context). |
| `CompleteResult` | §19.4 `CompleteResult` | Completion result carrying candidate values and totals. |
| `CompletionsCapability` | §19.1 The `completions` capability | Server capability declaring support for argument completion. |
| `ContentBlock` | §14.4 ContentBlock | Discriminated union of content block kinds exchanged in messages and results. |
| `CreateMessageRequest` | §21.2.4 Request Parameters | Deprecated sampling request asking the client to produce a model message. |
| `CreateMessageRequestParams` | §21.2.4 Request Parameters | Parameters of the deprecated sampling request (messages, model preferences, tools). |
| `CreateMessageResult` | §21.2.8 Result | Result of the deprecated sampling request carrying the generated message. |
| `CreateTaskResult` | §25.3 Task Augmentation of Existing Requests | Task-handle result (`resultType: "task"`) returned in place of an ordinary result. |
| `Cursor` | §3.7 Base Request and Notification Params | Opaque pagination cursor string. |
| `DetailedTask` | §25.4 Task and DetailedTask Object Types | Discriminated union of task objects with status-specific fields. |
| `DiscoverRequest` | §5.3.1 Request | Request for server discovery and protocol-revision negotiation. |
| `DiscoverResult` | §5.3.2 Result | Result of `server/discover` carrying the negotiated revision and capabilities. |
| `DiscoverResultResponse` | §5.3.2 Result | Success-response envelope wrapping a `DiscoverResult`. |
| `ElicitRequest` | §20.2 Delivery via input-required result | Input-request asking the client to collect user input via form or URL. |
| `ElicitRequestFormParams` | §20.3 Elicitation modes and parameter shapes | Form-mode elicitation parameters carrying the requested schema. |
| `ElicitRequestParams` | §20.2 Delivery via input-required result | Union of form-mode and URL-mode elicitation parameter shapes. |
| `ElicitRequestURLParams` | §20.3 Elicitation modes and parameter shapes | URL-mode elicitation parameters carrying the out-of-band URL and id. |
| `ElicitResult` | §20.5 ElicitResult and response actions | Elicitation response carrying the user action and any collected content. |
| `EmbeddedResource` | §14.4.5 EmbeddedResource | Content block embedding resource contents inline. |
| `EmptyResult` | §3.9 Empty Result | Result type with no fields beyond the base, used for bare acknowledgements. |
| `EnumSchema` | §20.4 The restricted form schema | Union of enumerated (single/multi-select) primitive form-field schemas. |
| `Error` | §3.8 Error Object | JSON-RPC error object (`code`, `message`, optional `data`). |
| `ExtensionSettings` | §24.3 Negotiation | Per-extension settings map carried during extension negotiation. |
| `FailedTask` | §25.4 Task and DetailedTask Object Types | `DetailedTask` variant for a task in the `failed` terminal state. |
| `GetPromptRequest` | §18.4 Getting a prompt: `prompts/get` | Request to resolve a prompt by name with arguments. |
| `GetPromptResult` | §18.4 Getting a prompt: `prompts/get` | Resolved prompt result carrying the message list. |
| `GetTaskRequest` | §25.7 Retrieving a Task: tasks/get | Request to retrieve a task's current detailed state by `taskId`. |
| `GetTaskResult` | §25.7 Retrieving a Task: tasks/get | Result carrying a `DetailedTask` for the requested task. |
| `Icon` | §14.2 Icon and Icons | Single icon descriptor (source, optional MIME type and size). |
| `Icons` | §14.2 Icon and Icons | Collection of icon descriptors. |
| `ImageContent` | §14.4.2 ImageContent | Content block carrying base64-encoded image data with a MIME type. |
| `Implementation` | §14.3 Implementation | Descriptor identifying an implementation (name, title, version). |
| `InputRequest` | §11.2 InputRequiredResult and the Input Requests | Discriminated union of input-request kinds a server may ask a client to fulfill. |
| `InputRequests` | §11.2 InputRequiredResult and the Input Requests | Map from server-chosen key to a single `InputRequest`. |
| `InputRequiredResult` | §11.2 InputRequiredResult and the Input Requests | Result (`resultType: "input_required"`) requesting further client input. |
| `InputRequiredTask` | §25.4 Task and DetailedTask Object Types | `DetailedTask` variant for a task awaiting client input. |
| `InputResponse` | §11.4 The Retry Request: InputResponseRequestParams | Discriminated union of input-response kinds answering an `InputRequest`. |
| `InputResponseRequestParams` | §11.4 The Retry Request: InputResponseRequestParams | Retry parameters carrying `inputResponses` and the echoed `requestState`. |
| `InputResponses` | §11.4 The Retry Request: InputResponseRequestParams | Map from key to `InputResponse`, answering the corresponding `inputRequests`. |
| `JSONArray` | §2.3 JSON Value Model | Ordered list of JSON values. |
| `JSONObject` | §2.3 JSON Value Model | Unordered, string-keyed map of JSON values. |
| `JSONRPCErrorResponse` | §3.5.2 Error Response | JSON-RPC error response envelope. |
| `JSONRPCMessage` | §3.1 JSON-RPC Framing | Union of all framed JSON-RPC message kinds. |
| `JSONRPCNotification` | §3.4 Notifications | JSON-RPC notification envelope (no id). |
| `JSONRPCRequest` | §3.3 Requests | JSON-RPC request envelope (with id). |
| `JSONRPCResponse` | §3.5 Responses | Union of success and error response envelopes. |
| `JSONRPCResultResponse` | §3.5.1 Success Response | JSON-RPC success response envelope carrying a result. |
| `JSONValue` | §2.3 JSON Value Model | Any JSON value (null, boolean, number, string, array, object). |
| `LegacyTitledEnumSchema` | §20.4 The restricted form schema | Deprecated enum form-field schema using a parallel `enumNames` array. |
| `ListPromptsRequest` | §18.2 Listing prompts: `prompts/list` | Paginated request to list available prompts. |
| `ListPromptsResult` | §18.2 Listing prompts: `prompts/list` | Paginated result listing prompts. |
| `ListResourcesRequest` | §17.2 Listing resources: `resources/list` | Paginated request to list available resources. |
| `ListResourcesResult` | §17.2 Listing resources: `resources/list` | Paginated, cacheable result listing resources. |
| `ListResourceTemplatesRequest` | §17.3 Listing resource templates: `resources/templates/list` | Paginated request to list resource templates. |
| `ListResourceTemplatesResult` | §17.3 Listing resource templates: `resources/templates/list` | Paginated, cacheable result listing resource templates. |
| `ListRootsRequest` | §21.1.4 The roots/list Input Request | Deprecated input-request asking the client for its root list. |
| `ListRootsResult` | §21.1.5 The ListRootsResult and the Root Type | Result of the deprecated roots listing. |
| `ListToolsRequest` | §16.2 Listing tools: `tools/list` | Paginated request to list available tools. |
| `ListToolsResult` | §16.2 Listing tools: `tools/list` | Paginated result listing tools. |
| `LoggingLevel` | §15.3.1 The LoggingLevel enumeration | Enumeration of syslog-style log severity levels. |
| `LoggingMessageNotification` | §15.3.2 The notifications/message notification | Notification carrying a log message from server to client. |
| `LoggingMessageNotificationParams` | §15.3.2 The notifications/message notification | Parameters of a logging notification (level, logger, data). |
| `MetaObject` | §4.1 The `_meta` Object | Open string-keyed metadata map carried in `_meta`. |
| `MissingRequiredClientCapabilityError` | §22.3.1 `-32003` MissingRequiredClientCapability | Error payload reporting a required client capability that was not declared. |
| `ModelHint` | §21.2.9 Model Preferences | Hint guiding model selection during deprecated sampling. |
| `ModelPreferences` | §21.2.9 Model Preferences | Model-selection preferences for deprecated sampling. |
| `Notification` | §3.4 Notifications | Base shape of a notification (method and optional params). |
| `NotificationParams` | §3.7 Base Request and Notification Params | Base parameters shape common to notifications. |
| `NumberSchema` | §20.4 The restricted form schema | Primitive form-field schema describing a numeric input. |
| `OpenLinkParams` | §26.5.3 Tool-invocation and other requests (UI → Host) | UI-to-host request parameters to open an external link. |
| `PaginatedRequestParams` | §12.2 Request and Result Shapes | Base request parameters carrying an optional `cursor`. |
| `PaginatedResult` | §12.2 Request and Result Shapes | Base result carrying an optional `nextCursor`. |
| `PrimitiveSchemaDefinition` | §20.4 The restricted form schema | Union of primitive form-field schema kinds (string, number, boolean, enum). |
| `ProgressNotification` | §15.1.3 The notifications/progress notification | Notification reporting progress on a long-running request. |
| `ProgressNotificationParams` | §15.1.3 The notifications/progress notification | Parameters of a progress notification (token, progress, total, message). |
| `ProgressToken` | §3.7 Base Request and Notification Params | Token correlating progress notifications with a request. |
| `Prompt` | §18.3 The `Prompt` and `PromptArgument` types | Descriptor of an available prompt and its arguments. |
| `PromptArgument` | §18.3 The `Prompt` and `PromptArgument` types | Descriptor of a single prompt argument. |
| `PromptListChangedNotification` | §18.6 The prompts-list-changed notification | Notification that the prompt list has changed. |
| `PromptMessage` | §18.5 The `PromptMessage` type and valid content | Single message within a resolved prompt. |
| `PromptReference` | §19.3 Reference types: `PromptReference` and `ResourceTemplateReference` | Completion reference identifying a prompt. |
| `PromptsCapability` | §18.1 The `prompts` capability | Server capability declaring support for prompts. |
| `ProtectedResourceMetadata` | §23.2 Protected Resource Metadata Discovery | Metadata document advertising the resource server's authorization servers. |
| `ReadResourceRequest` | §17.5 Reading a resource: `resources/read` | Request to read a resource by URI. |
| `ReadResourceRequestParams` | §17.5 Reading a resource: `resources/read` | Parameters of a resource-read request (URI plus input responses). |
| `ReadResourceResult` | §17.5 Reading a resource: `resources/read` | Cacheable result carrying the read resource's contents. |
| `Request` | §3.3 Requests | Base shape of a request (method and optional params). |
| `RequestId` | §3.2 Request Identifier | Request-correlation identifier (string or number). |
| `RequestMetaObject` | §4.3 Protocol-Defined Per-Request `_meta` Keys | `_meta` shape for protocol-defined per-request metadata keys. |
| `RequestParams` | §3.7 Base Request and Notification Params | Base parameters shape common to requests, carrying `_meta`. |
| `RequestProtocolVersionMeta` | §5.2 Carrying the Protocol Revision on a Request | `_meta` shape carrying the protocol revision on a request. |
| `Resource` | §17.4 The `Resource` and `ResourceTemplate` types | Descriptor of a concrete resource. |
| `ResourceContents` | §14.5 ResourceContents and variants | Base of the resource-contents variants (text/blob). |
| `ResourceLink` | §14.4.4 ResourceLink | Content block referencing a resource by URI. |
| `ResourceListChangedNotification` | §17.7 Change notifications and subscriptions | Notification that the resource list has changed. |
| `ResourceNotFoundError` | §17.6 Resource-not-found error | Error payload reporting that a requested resource URI was not found. |
| `ResourcesServerCapability` | §17.1 The `resources` capability | Server capability declaring support for resources (and subscription flags). |
| `ResourceTeardownParams` | §26.5.4 Lifecycle and context-change messages (Host → UI) | Host-to-UI parameters signalling that the UI resource is being torn down. |
| `ResourceTemplate` | §17.4 The `Resource` and `ResourceTemplate` types | Descriptor of a parameterized resource URI template. |
| `ResourceTemplateReference` | §19.3 Reference types: `PromptReference` and `ResourceTemplateReference` | Completion reference identifying a resource template. |
| `ResourceUiMeta` | §26.4 The UI Resource | UI metadata (CSP, permissions) attached to a UI resource. |
| `ResourceUpdatedNotification` | §17.7 Change notifications and subscriptions | Notification that a subscribed resource has been updated. |
| `ResourceUpdatedNotificationParams` | §17.7 Change notifications and subscriptions | Parameters of a resource-updated notification (URI). |
| `Result` | §3.6 Result Base Type | Base of all result types, carrying `resultType` and `_meta`. |
| `ResultType` | §3.6 Result Base Type | Open discriminator selecting the concrete result shape. |
| `Role` | §14.7 Role | Message-author role (`user` or `assistant`). |
| `Root` | §21.1.5 The ListRootsResult and the Root Type | Deprecated descriptor of a client-exposed filesystem root. |
| `SamplingMessage` | §21.2.6 Messages and Content Blocks | Single message in a deprecated sampling conversation. |
| `SamplingMessageContentBlock` | §21.2.6 Messages and Content Blocks | Content-block union for sampling messages (text/image/audio plus the sampling-only `tool_use`/`tool_result` blocks; excludes `resource_link` and `resource`). |
| `SandboxResourceReadyParams` | §26.5.5 Host-internal sandbox-proxy messages | Host-internal sandbox-proxy parameters signalling the UI resource is ready. |
| `ServerCapabilities` | §6.3 ServerCapabilities | Capability set a server advertises to the client. |
| `SingleSelectEnumSchema` | §20.4 The restricted form schema | Union of single-select enum form-field schema variants. |
| `SizeChangedParams` | §26.5.4 Lifecycle and context-change messages (Host → UI) | Host-to-UI parameters reporting a UI size change. |
| `StringSchema` | §20.4 The restricted form schema | Primitive form-field schema describing a string input. |
| `SubscriptionFilter` | §10.2 The `subscriptions/listen` Request and the Notification Filter | Filter selecting which notification kinds a subscription delivers. |
| `SubscriptionsAcknowledgedNotification` | §10.3 Acknowledgement | Notification acknowledging an established subscription. |
| `SubscriptionsAcknowledgedNotificationParams` | §10.3 Acknowledgement | Parameters of the subscription-acknowledgement notification. |
| `SubscriptionsListenRequest` | §10.2 The `subscriptions/listen` Request and the Notification Filter | Request to open a server-to-client notification stream. |
| `SubscriptionsListenRequestParams` | §10.2 The `subscriptions/listen` Request and the Notification Filter | Parameters of the subscription-listen request (filter). |
| `Task` | §25.4 Task and DetailedTask Object Types | Core task object (id, status, timestamps) shared by all task variants. |
| `TaskStatus` | §25.5 Task Status Lifecycle | Enumeration of task lifecycle states. |
| `TaskStatusNotification` | §25.10 Task Status Notifications: notifications/tasks | Notification reporting a task's status change. |
| `TaskStatusNotificationParams` | §25.10 Task Status Notifications: notifications/tasks | Parameters of a task-status notification (a `DetailedTask`). |
| `TasksExtensionCapability` | §25.2 Capability Declaration and Negotiation | Capability declaring support for the Tasks extension. |
| `TextContent` | §14.4.1 TextContent | Content block carrying plain text. |
| `TextResourceContents` | §14.5 ResourceContents and variants | Resource contents variant carrying text. |
| `TitledMultiSelectEnumSchema` | §20.4 The restricted form schema | Multi-select enum form-field schema with per-option titles. |
| `TitledSingleSelectEnumSchema` | §20.4 The restricted form schema | Single-select enum form-field schema with per-option titles. |
| `Tool` | §16.3 The `Tool` type | Descriptor of an available tool (name, schemas, annotations). |
| `ToolAnnotations` | §16.7 Tool annotations | Behavioral hints about a tool (read-only, destructive, idempotent, etc.). |
| `ToolCancelledParams` | §26.5.2 Tool input and result delivery (Host → UI) | Host-to-UI parameters signalling a tool invocation was cancelled. |
| `ToolChoice` | §21.2.5 Tool Choice | Deprecated sampling control selecting how tools may be used. |
| `ToolInputParams` | §26.5.2 Tool input and result delivery (Host → UI) | Host-to-UI parameters delivering tool input arguments. |
| `ToolListChangedNotification` | §16.8 The `notifications/tools/list_changed` notification | Notification that the tool list has changed. |
| `ToolResultContent` | §21.2.6 Messages and Content Blocks | Sampling content block carrying a tool result. |
| `ToolResultParams` | §26.5.2 Tool input and result delivery (Host → UI) | Host-to-UI parameters delivering a tool result. |
| `ToolsCallParams` | §26.5.3 Tool-invocation and other requests (UI → Host) | UI-to-host parameters requesting a tool invocation. |
| `ToolsCapability` | §16.1 The `tools` server capability | Server capability declaring support for tools. |
| `ToolUiMeta` | §26.3 Declaring a UI on a Tool | UI metadata declaring an interactive UI on a tool. |
| `ToolUseContent` | §21.2.6 Messages and Content Blocks | Sampling content block carrying a tool-use request. |
| `TraceContextMeta` | §15.4.1 Reserved trace-context metadata keys | `_meta` shape carrying W3C trace-context fields. |
| `UiContentSecurityPolicy` | §26.4 The UI Resource | Content-security-policy descriptor for a UI resource. |
| `UiHostContext` | §26.5.1 Initialization handshake | Host rendering context (theme, display mode, styles) supplied to a UI. |
| `UiHostExtensionCapability` | §26.2 Extension Identifier and Capability Negotiation | Capability declaring support for the interactive user-interface extension. |
| `UiInitializeParams` | §26.5.1 Initialization handshake | UI-to-host initialization request parameters. |
| `UiInitializeResult` | §26.5.1 Initialization handshake | Host-to-UI initialization result (granted permissions, CSP, host context). |
| `UiMessageParams` | §26.5.3 Tool-invocation and other requests (UI → Host) | UI-to-host parameters carrying a user-facing message. |
| `UiPermissions` | §26.4 The UI Resource | Sandbox permission set requested or granted for a UI resource. |
| `UnsupportedProtocolVersionError` | §22.3.2 `-32004` UnsupportedProtocolVersion | Error payload reporting that no mutually supported protocol revision exists. |
| `UntitledMultiSelectEnumSchema` | §20.4 The restricted form schema | Multi-select enum form-field schema without per-option titles. |
| `UntitledSingleSelectEnumSchema` | §20.4 The restricted form schema | Single-select enum form-field schema without per-option titles. |
| `UpdateModelContextParams` | §26.5.3 Tool-invocation and other requests (UI → Host) | UI-to-host parameters updating the model-visible context. |
| `UpdateTaskRequest` | §25.8 Supplying Input to a Task: tasks/update | Request supplying input responses to an in-progress task. |
| `UpdateTaskResult` | §25.8 Supplying Input to a Task: tasks/update | Empty acknowledgement returned for a task update. |
| `WorkingTask` | §25.4 Task and DetailedTask Object Types | `DetailedTask` variant for a task in the `working` state. |
