# RISClet Compiler

RISClet is a lightweight compiler for a custom domain-specific language (DSL) targeting the AArch64 instruction set. Designed for embedded environments, it produces minimal binaries and avoids standard libraries entirely.

---

## Features

- Custom parser using if/switch (no parser generators)
- Tuple-based Intermediate Representation (IR)
- Emits raw AArch64 `.s` assembly files
- No dynamic memory allocation
- Static `.data`-based variable storage
- Optional GUI (Electron-based IDE) for writing and compiling RISClet code

---

## Example

**RISClet source:**
```
x: Int32 = 1 + 2;
Output(x);
```

**Tuple IR:**
```
(ADD, x, 1, 2)
(OUT, x)
```

**AArch64 Output:**
```
mov w0, #1
mov w1, #2
add w2, w0, w1
ldr x0, =x
str w2, [x0]
ldr x0, =x
ldr w0, [x0]
bl Output
```

---

## Usage

### CLI Compiler

**Please note that this is currently placeholder**

1. Build the compiler:
   dotnet build

2. Compile a RISClet file:
   ./RISCletCompiler source.rlt -o output.s

### GUI (IDE)

1. Navigate to the IDE folder:
   cd IDE

2. Install dependencies:
   npm install

3. Run the IDE:
   npm run dev

---

## License

MIT License — see LICENSE file for details.

---

## Notes

This project was developed as part of a systems-level programming coursework component. It focuses on minimal, predictable compilation without the complexity or size of traditional standard libraries.
