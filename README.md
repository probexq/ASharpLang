# A# (A-Sharp)

The official repo for the A# language, supposed to be a hobby project developed by me.

A# is an open-source modern math-first .NET programming language, aimed to make heavy math on the framework much easier.
It's a pretty lightweight compared to Julia or even Python (for now at least).
A# is statically typed and converts to CIL (Common Intermediate Language).
Note: the language uses the .ash file extension.

## Why A#?

A# removes the verbosity of C#, making it easier to develop and calculate things.

**C#:**

```csharp

double result = Math.Sqrt(Math.Max(a, b));

```

**A#:**

```asharp

let result = _(+#(a, b)),

```

---

## 3. Features (v0.1.5)

* **Direct IL Generation**
* **Short Startup(Usually less than 0.5s)** Perfect for budget systems.
* **In works/working Error Refining** Line/Column reporting for bad syntax, missing operands etc.
* **Compatible with PATH**

---

## 4 Roadmap

* **0.1.0+(Done)** log() for terminal output, basic syntax(normal +-*/^, let for variable declaration, |a| for Math.Abs (Modulus), $ for importing/including etc.)
* **0.2.0+** Guard-based logic(new condition type), constants, optimizations and small fixes.
* **0.3.0+** Lambdas, ? Syntax for Math.Random, optimizations and fixes.
* **0.4.0+** logf() for string support, elog() for Exceptions, !! syntax for error-catching.

## End of README

I really hope this gets traction because the concept of A# is pretty niche, neither i am trying to replace C#, i am trying to supercharge .NET.
If there are any errors you can open an issue and i will be very thankful for any support or help i get.
