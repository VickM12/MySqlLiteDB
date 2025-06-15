# MySqlLiteDb

I wanted to learn how to build a Structured Query Language (SQL) database engine…  
so I started building one.

---

## What This Is

**MySqlLiteDb** – a **zero-dependency, file-backed SQL toy** forged in C#.

**Current feats**

| Feature | Notes |
| ------- | ----- |
| `CREATE TABLE`, `INSERT` (positional **and** named), `SELECT *` | |
| `UPDATE`, `DELETE` | Single-column equality predicates |
| Types | `GUID`, `TEXT`, `INT` |
| Storage | Flat `.tbl` files; first line is the schema header |

---

## Why I Built It

I wanted to see *every* gear turn:

1. **Lexer** – slices raw SQL into tokens.  
2. **Parser** – turns tokens into instructions.  
3. **Storage** – saves rows, rewrites tables.  
4. **Engine** – command loop that ties it all together.

Learning > polish. Expect sharp edges.

---

## 🚀 Quick Start

```bash
git clone https://github.com/<you>/MySqlLiteDb
cd MySqlLiteDb
dotnet run
