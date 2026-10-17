# Ritm — scheduled breaks

**ContractorControl** is a modern **microservices orchestration** platform built on the principles of **Clean Architecture**. It serves as an **event-driven orchestration** system and a **passive registrar** for **business process tracking**, offering a lightweight **BPM alternative** (such as Camunda).

The core of the system relies on a **DAG (Directed Acyclic Graph)** model to ensure a **deterministic process** without cycles. To achieve maximum performance, **state validation** is handled via optimized **stored procedures** on the database side.

The platform is ideal for high-throughput distributed systems, data pipelines, and strict sequential approvals where process determinism is critical.

### Technology Stack
* **Backend:** **.NET 10** / **dotnet10**, **C#**, **ASP.NET Core API**
* **Architecture Patterns:** **MediatR (CQRS library)**
* **Data Layer:** **PostgreSQL**, **EF Core (Entity Framework Core)**
* **Deployment & Tools:** **Docker Compose**, **Swagger UI**

### Key Components
* **Dynamic CRUD:** A universal service for dynamic data management (`insert`, `update`, `delete`).
* **GenerationCC:** A dedicated **CLI graph generator** for building graph connections.

## Service documentation

`DayBit` turns a day of the week into a single bit. The seven days of a schedule are not stored as a list, but as one number, `DaysMask`: each bit means “this day is on”.

```5:15:desktop/Ritm.Core/WindowClock.cs
public static int DayBit(DayOfWeek day) => day switch
{
    DayOfWeek.Monday => 1 << 0,
    DayOfWeek.Tuesday => 1 << 1,
    DayOfWeek.Wednesday => 1 << 2,
    DayOfWeek.Thursday => 1 << 3,
    DayOfWeek.Friday => 1 << 4,
    DayOfWeek.Saturday => 1 << 5,
    DayOfWeek.Sunday => 1 << 6,
    _ => 0,
};
```

`1 << n` shifts a one `n` positions to the left. The result is a power of two, and in binary exactly one bit is set:

| Day | Shift | Number | Bit |
|---|---|---|---|
| Monday | `1 << 0` | 1 | `0000001` |
| Tuesday | `1 << 1` | 2 | `0000010` |
| Wednesday | `1 << 2` | 4 | `0000100` |
| Thursday | `1 << 3` | 8 | `0001000` |
| Friday | `1 << 4` | 16 | `0010000` |
| Saturday | `1 << 5` | 32 | `0100000` |
| Sunday | `1 << 6` | 64 | `1000000` |

The order is its own, not the same as `DayOfWeek`. In .NET the week starts on Sunday (`Sunday = 0`). Here the zero bit is Monday, because the checkboxes in the window run Mon…Sun, and bit `i` matches the checkbox index `1 << i`.

This is a switch expression: a branch returns a value immediately, and `break` is not needed. The `_ => 0` branch is theoretically unreachable — `DayOfWeek` has only seven values — but without it the expression does not cover every case and does not compile.

A schedule mask is the bitwise OR of these bits. Mon–Fri:

```text
0000001 | 0000010 | 0000100 | 0001000 | 0010000 = 0011111 = 31 = 0b0011111
```

The check “today is a day of this schedule” is a bitwise AND. The bit is present if the result is not zero:

```csharp
(schedule.DaysMask & DayBit(now.DayOfWeek)) != 0
```

For the mask `0011111` (Mon–Fri) and Thursday `0001000`: `0011111 & 0001000 = 0001000` — the day is included. For Saturday `0100000`: `0011111 & 0100000 = 0` — it is not included.

It is done this way because in SQLite the days are one integer field, not seven rows. Turning a day on or off is `mask ^= (1 << i)`: XOR flips exactly that bit and does not touch the others.

A zero mask means “no days selected”, and `Contains` returns `false` immediately. For a window that crosses midnight, the bit is taken from yesterday if the current time is still the “tail” of the window: Thursday 22:00–06:00 is checked as Thursday at 05:00 on Friday, `DayBit(now.AddDays(-1).DayOfWeek)`.
