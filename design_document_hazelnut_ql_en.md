# DESIGN DOCUMENT - Hazelnut_QL

**Project:** Custom GraphQL Execution Engine

## 1. General Overview and System Goals

* **Purpose:** A standalone algorithmic class library for lexical analysis, syntactic parsing, static validation, and asynchronous execution of GraphQL queries without dependencies on databases, HTTP, or third-party frameworks.
* **Inputs:** GraphQL query string (`document string`), schema graph (`Schema`), variables dictionary (`variables`).
* **Outputs:** Execution result (`ExecutionResult`), containing a results dictionary (`data`) and/or a list of errors (`errors`), guaranteed to comply with the official GraphQL specification and the selection tree.
* **Main Entry Point:** `GraphQLEngine` — a Facade class encapsulating `Lexer`, `Parser`, `Validation Engine`, and `ExecutionEngine`. The end consumer interacts with only a few core types to build the schema and trigger execution.

## 2. Functional Requirements

### 2.1. Lexical and Syntactic Analysis (Compiler Frontend)

1. **Tokenization (Lexer):**
   * Recognition of punctuation marks (`{`, `}`, `(`, `)`, `:`, `$`, `@`, `...`, `!`, `[`, `]`, etc.).
   * Recognition of keywords (`query`, `mutation`).
   * Recognition of literals: strings (`"..."`), integers (`Int`), floating-point numbers (`Float`), boolean values (`true`/`false`), `null`.
   * Tracking exact character coordinates (line, column) for generating error messages.
   * Ignoring comments (`# ...`), whitespace, and commas.

2. **AST Construction (Parser):**
   * Building a typed Abstract Syntax Tree (AST).
   * Support for operations (`query`, `mutation`).
   * Support for fields (`Field`), nested selections (`SelectionSet`), aliases (`Alias`), and arguments (`Arguments`).
   * Support for fragments: named fragments (`FragmentDefinition`) and inline fragments (`Inline Fragment`).
   * Support for variable declarations (`VariableDefinitions`).
   * Support for directives (`@skip`, `@include`).

### 2.2. Type System and Schema Builder (Reflection-Free)

1. **Built-in Scalar Types:** `Int`, `Float`, `String`, `Boolean`, `ID`.
2. **Type Modifiers (Wrapper Types):**
   * `NonNullType` (guarantees non-null values).
   * `ListType` (lists/arrays).
3. **Complex and Polymorphic Types:**
   * `ObjectType` (hierarchical structures with a set of typed fields and arguments).
   * `UnionType` (a union of multiple object types with a concrete type resolution function `ResolveType`).
4. **Configurator (Fluent API):**
   * Declarative syntax for describing the schema in code.
   * Explicit assignment of resolver delegates (`ctx => ...`) to each field without "magical" class scanning via reflection.

### 2.3. Algorithmic Validation (Validation Engine)

1. **Schema and Structure Checks:**
   * Fields exist on the types of the current selection.
   * Leaf Field Selections requirement: scalar fields cannot have nested selections, whereas object fields must have them.
2. **Fragment Checks:**
   * Fragments cannot be applied to scalars.
   * Type compatibility between the fragment and the calling context (*Type condition*).
   * Fragment cycle detection: identifying mutual and recursive fragment spreads.
3. **Variable Checks:**
   * All declared variables must be used.
   * All used variables must be declared.
   * Type matching between passed variables and field argument signatures.

### 2.4. Execution Core (Execution Engine)

1. **Variable Preparation (Coercion):** Validation of the external JSON variables dictionary, type checking, and applying default values.
2. **Operation Execution:**
   * `Query`: asynchronous parallel execution of root-level fields.
   * `Mutation`: strict sequential execution of top-level fields to prevent race conditions.
3. **Selection Merging:** Merging identical fields and fragments at the same level into a single result.
4. **Directive Processing:** Evaluating `@include(if: ...)` and `@skip(if: ...)` directives before executing a node.
5. **Null Bubbling (Bubble-up Nullability):** If a field typed as `NonNull` returns `null` or throws an exception, `null` bubbles up to the nearest parent field that allows nullable values.

## 3. How It Works (Internal Processing Pipeline)

The lifecycle of each query consists of 5 sequential steps where data flows strictly in one direction:

```mermaid
graph TD
    A[Query Document] --> B[1. Lexer]
    B --> C[Tokens]
    C --> D[2. Parser]
    D --> E[AST]
    F[Variables Dictionary] --> G[3. Variable Coercion]
    E --> H[4. Validator (Visitor)]
    G --> H
    H -- "(If validation succeeds)" --> I[5. Execution Engine]
    J[Schema + Resolvers] --> I
    I --> K[ExecutionResult (JSON)]
```

### Step 1: Lexical Analysis (Lexer)
* **Algorithm:** Single-pass Finite State Machine (FSM).
* **Operation:**
  * Reads the input string character by character.
  * Discards insignificant characters (whitespace, commas, line breaks).
  * Determines the token type based on the first character (e.g., `"` starts reading a string, a digit or `-` starts a number, a letter starts an identifier or keyword).
  * Outputs a sequence of `Token` objects.

### Step 2: Syntactic Analysis (Parser)
* **Algorithm:** Recursive Descent Parsing with 1-token lookahead.
* **Operation:**
  * The `ParseDocument()` method calls `ParseDefinition()`, which determines the node type (operation or fragment) based on keywords.
  * Selectors are then parsed recursively: `ParseSelectionSet()` $\rightarrow$ `ParseField()` $\rightarrow$ `ParseArguments()`.
  * Forms a hierarchical tree of acyclic AST (*Abstract Syntax Tree*) objects with `DocumentNode` as the root.

### Step 3: Variable Coercion
* **Operation:**
  * Before execution begins, the engine compares the dictionary of passed parameters (JSON) against the declared variables in the operation's AST node (`VariableDefinitions`).
  * Checks type compatibility (e.g., whether a string `"123"` or number `123` can be coerced to the `Int` scalar).
  * If a required variable (`NonNull`) is missing, an error is generated; if an optional variable is missing but has a default value, the default value is substituted.

### Step 4: Static Validation (Validation Engine)
* **Algorithm:** Visitor pattern with Depth-First Search (DFS).
* **Operation:**
  * The validator traverses the AST alongside the schema types.
  * A chain of validation rules (*Rule Set*) is applied to each node.
  * **Fragment Cycle Detection:** A directed graph of fragment spreads `Fragment A -> Fragment B` is constructed. A DFS algorithm with three-color vertex marking (*White* — unvisited, *Gray* — currently on the recursion stack, *Black* — completely processed) is applied. If the algorithm encounters a "gray" vertex, a cycle is detected, and a validation error is generated.
  * If at least one error is found, query execution is halted before any resolvers are invoked (protecting the system from unnecessary load).

### Step 5: Query Execution (Execution Engine)
* **Algorithm:** Recursive asynchronous evaluation of the selection tree (*Selection Set Execution*).
* **Operation:**
  1. **Root Level:**
     * If `Query`: all top-level fields are resolved in parallel (`Task.WhenAll`).
     * If `Mutation`: root fields are resolved strictly sequentially using `await`.
  2. **Field Evaluation:**
     * Directives (`@skip` / `@include`) are checked. If the condition dictates skipping, the field is ignored entirely.
     * The resolver bound in the schema is invoked: `resolver(fieldExecutionContext)`.
     * The returned raw data becomes the `Source` for nested selections.
  3. **Result Type Handling:**
     * **Scalar:** written to the final dictionary under the field's name (or its alias).
     * **Object:** execution of its child `SelectionSet` is invoked recursively.
     * **List:** subfields are resolved recursively for each item in the list, forming the final array.
     * **Union:** the concrete `ObjectType` is determined via `ResolveType(source)`, after which inline fragments are filtered for that specific type.
  4. **Null Bubbling Rule (Bubble-up):**
     * If a resolver for a field marked as `NonNull` returns `null` or throws an exception, an error is added to the `errors` list, and the field's value is forced to `null`.
     * Because it is `NonNull`, it invalidates the parent object: the parent object is replaced with `null`.
     * This process recursively "bubbles up" until it reaches a field that allows nullable values (or, in the worst case, the entire `data` block becomes `null`).

## 4. Testing Strategy

Since the project is a pure algorithmic core, quality is guaranteed through three testing tiers and comprehensive End-to-End scenarios:

1. **Unit Testing:** Isolated checks for each class:
   * Lexer tests for edge cases (unterminated strings, unexpected EOF, Unicode).
   * Parser tests for accurate AST node construction.
   * Query execution tests verifying proper `null` bubbling.

2. **Property-Based Testing** (using *FsCheck* or similar libraries):
   * Generation of random valid queries and schemas to verify invariants: *"The structure of the output JSON always mirrors the AST selection tree"*.
   * Testing validator resilience against extreme nesting depths (*StackOverflow protection*).

3. **Mutation Testing** (*Stryker.NET*):
   * Automated injection of mutations into the core code (replacing `>` with `>=`, removing `if` conditions, inverting boolean values).
   * Verification that all artificial bugs are caught by the existing unit test suite (aiming for the highest possible *Mutation Score*).

4. **End-to-End (Coercion $\rightarrow$ Validator $\rightarrow$ Execution Result):**

| Scenario Category | Test Schema / Query Description | What Is Verified |
| :--- | :--- | :--- |
| **Basic Queries** | Schema with fields of type `User(id, name, age)`. Queries for individual scalar fields, nested objects, and `[User]` lists. | Accurate JSON tree construction, field renaming via `Alias`, and support for passing inline arguments. |
| **Parallel vs. Sequential Execution** | Root `Query` with resolvers having an artificial `Task.Delay`. Root `Mutation` incrementing an in-memory counter. | Guarantee that `Query` executes in parallel (`Task.WhenAll`), while mutations run strictly sequentially without race conditions. |
| **Variables & Coercion** | Operation with `limit: Int = 10`, `filter: String!`. Passing a variables dictionary via input parameters. | Default value substitution, validation of missing required variables, and type coercion. |
| **Fragments & Inline Fragments** | Queries reusing `fragment UserFields on User`, as well as overlapping fields (*Selection Merging*). | Proper fragment expansion without duplicating requested data. |
| **Polymorphism (Union Types)** | Schema with `SearchResult = Union(User, Article)`. Query with inline fragments `... on User`, `... on Article`. | Behavior of the runtime `ResolveType` function and filtering of matching selections by concrete type. |
| **Directives (`@skip`, `@include`)** | Query with fields annotated with `@include(if: $flag)` and `@skip(if: $flag)` directives. | Complete omission of AST nodes and non-execution of corresponding resolvers under false/true conditions. |
| **Null Bubbling (Bubble-up Nullability)** | Resolver of a required field `name: String!` returns `null` or throws an `Exception`. | Error is recorded in the `errors` array, and `null` recursively bubbles up to the nearest nullable ancestor. |
| **E2E Negative Validation Tests** | Syntactically valid queries that violate schema rules (non-existent fields, scalar with a selection set, cyclic fragments `A -> B -> A`). | `ExecutionResult.Data == null`, presence of specific errors in `Errors`, and resolvers are never invoked. |