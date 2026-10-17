<<<<<<< HEAD
# Ritm
=======
# Ritm — scheduled breaks
>>>>>>> 9492aa463bd5615b5f2569ed2729a16f3ca0027b

Scheduled breaks for a working day.

**Document:** product description and operating note  
**Product version:** 1.1  
**Language of this document:** English  
**Language of the program:** Russian

Ritm reminds a person to pause on a chosen interval, but only while a working window is open. A typical use is a break every 45 minutes between 09:00 and 19:00. The reminder is a system sound and a message written for that counter.

There is no account, no installer, and no shared service. Each copy keeps its own board on the machine where it runs.

## What it is for

Ritm is for a person who works to a clock and wants the pause to belong to that clock.

<<<<<<< HEAD
- The working time is a schedule: a from–until range and the weekdays it applies to.
- Inside one schedule there can be several counters. Each counter has its own interval, in minutes, and its own message.
- Outside the schedule the counters do not move. Closing or minimising the window does not stop them.
- When a counter reaches its interval, the system sound plays and that counter’s message is shown. The count then starts again.

The Windows program and the Linux program are meant to stay on the computer through the working day, including from the tray. The browser copy is the same board for a session in that browser. It is not a control panel for the desktop program.

## Editions

These archives are separate products. They share the same idea. They do not share data.

| Archive | What it is | Where it runs |
|---|---|---|
| `Ritm-tray-windows.zip` | Tray application, version 1.1, ready to run. .NET 9 is already inside. | Windows 10 and Windows 11, 64-bit |
| `Ritm-tray-source.zip` | Source of that application. Needed only to compile again. | Build machine with the .NET 9 SDK |
| `Ritm-linux-kde-gnome.zip` | Desktop application for KDE and GNOME. The settings window is a web page. The counters run in the program. | KDE and GNOME, with Python 3 |
| `Ritm-site-source.zip` | Source of the browser site. This is not the Linux program. | A machine with Node.js 22 |
| `Ritm-site-build.zip` | The browser site, already built. `npm install` is not required. | A machine with Node.js 22 |

The website does not write settings into the tray application, and the tray application does not read the website. They are two separate copies of the same board.

- The desktop application keeps its data in a file on the computer.
- The site keeps its data in the browser where it was opened.

## How a working day is described

A **schedule** is a time window plus weekdays. Example: 09:00 to 19:00, Monday to Friday.

- If “until” is earlier than “from”, the window crosses midnight.
- 09:00–00:00 means until midnight.
- One schedule can hold several counters. The interval of a counter is in minutes.
- Once a second the program checks every schedule. While the current time is inside the window, enabled counters of that schedule add one second. Outside the window they stay still.
- When a counter reaches its interval, the sound plays and that counter’s message is shown. The count starts over.
- **Snooze 5 min** sets the next alert five minutes later.
- **Reset at the start of the window** starts counters from zero the first time the window is entered on a new day.
- **Test** shows the message and plays the sound immediately.
- **Pause** stops every schedule until it is turned off. Pause is not kept after a restart.

## Data

Nothing in these editions is sent to an account. There is no sign-in.

| Edition | Where the board is kept |
|---|---|
| Windows tray application | `%LOCALAPPDATA%\Ritm\ritm.db`, usually `C:\Users\YourName\AppData\Local\Ritm\ritm.db` |
| KDE and GNOME application | `~/.local/share/ritm/ritm.db` |
| Browser site | the browser, `localStorage` key `ritm-v1` |

On Windows, **Data folder** opens the folder above. `error.log` is written there if a tick fails.

To start the Windows program from an empty database, quit from the icon menu and delete `ritm.db`. The next start creates a new database.

Another browser, another computer, or clearing site data starts the browser board over. Nothing from the site is written into the tray application’s file. Linux data is separate from Windows data.

## What this set does not do

- It does not synchronise the website, the Windows program, and the Linux program.
- It does not keep Pause after a restart.
- It does not install itself. The Windows build is unpacked and run. The file is not signed. Windows may ask whether an unrecognised application is trusted. That is normal for this build.
- It does not open a second copy of the Windows program. If it is already running, a message says Ritm is already running and the icon is by the clock.
- It does not provide an English interface. Menus and checks use the Russian labels named below.

## 1. Tray application

Archive: `Ritm-tray-windows.zip`.

1. Unpack the archive into a folder, for example `C:\Ritm`. Unpack the whole folder. `Ritm.exe` must stay next to the dll files. `Ritm.exe` alone, pulled out of the archive, will not start. Do not run the program from inside the archive window.
2. Run `Ritm.exe`. There is no installer.
3. The close button and minimise hide the window in the tray by the clock. The counters keep running. If the icon is not visible, open the arrow **Show hidden icons** by the clock.
4. Quit only from the icon menu: right-click, then **Exit**. Closing the window does not quit the program.

### Autostart

The check **Автозапуск при входе в Windows** is in two places:

- the window menu, under **Окно**;
- the tray icon menu, on a right-click.

When the mark is on, Windows starts Ritm in the tray at login and does not open the window. When the mark is off, the startup entry is removed.

Registry: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, value name `Ritm`.

## 2. Build the tray application from source

Archive: `Ritm-tray-source.zip`. This is not required for normal use. The ready build is `Ritm-tray-windows.zip`.

The .NET 9 SDK is required, not only the runtime: [https://dotnet.microsoft.com/download/dotnet/9.0](https://dotnet.microsoft.com/download/dotnet/9.0)

Unpack the archive, open that folder in a terminal, and run:

```text
dotnet run --project Ritm.Check
dotnet publish Ritm.App\Ritm.App.csproj -c Release -r win-x64 --self-contained true -o publish
```

On macOS or Linux use forward slashes:

```text
dotnet publish Ritm.App/Ritm.App.csproj -c Release -r win-x64 --self-contained true -o publish
```

The first command checks the clock and the schedule windows. The second writes the program into the `publish` folder. Run `publish\Ritm.exe`. `Ritm-tray-windows.zip` was produced by that same command.

`Ritm.exe` runs only on Windows 10/11, 64-bit. The publish command can be run on Linux or macOS if the .NET 9 SDK is installed: the project sets `EnableWindowsTargeting`. The resulting exe still has to be copied to Windows.

## 3. Website from source

Archive: `Ritm-site-source.zip`.

Node.js 22 is required: [https://nodejs.org/](https://nodejs.org/)

1. Unpack the archive.
2. In that folder run:

```text
npm install
npm run dev
```

3. Open [http://localhost:8080](http://localhost:8080) in a browser.

Stop the server with Ctrl+C in that terminal.

To build the site again from this same folder:

```text
npm run build
```

The built folder is `.vercel/output` (`.vercel\output` on Windows). It is easier to use the ready archive `Ritm-site-build.zip`.

## 4. Website, already built

Archive: `Ritm-site-build.zip`.

Node.js 22 is required. `npm install` is not required.

Windows: unpack the archive and run `start.cmd`.

macOS and Linux: unpack the archive and, in that folder, run:

```text
sh start.sh
```

Open [http://127.0.0.1:3000](http://127.0.0.1:3000). Stop the server with Ctrl+C in the window where it is running.

Another port, Windows (`cmd`):

```text
set PORT=8080
start.cmd
```

macOS and Linux:

```text
PORT=8080 sh start.sh
```

The server listens only on this computer (`127.0.0.1`). Schedules stay in the browser, not in the build folder.

## 5. KDE and GNOME application

Archive: `Ritm-linux-kde-gnome.zip`.

This is a separate program, not the Windows file. Counters, schedules, sound, and messages work the same way. The settings window is the web desk of this program. If Chrome, Chromium, Edge, or Brave is installed, it opens as its own browser window. Otherwise it opens in the default browser through `xdg-open`.

Python 3 is required. Most KDE and GNOME systems already have it.

1. Unpack the archive.
2. Start it with `sh start.sh` or `python3 ritm.py`.
3. The window is the schedule board. Closing the window does not stop the counters.
4. Quit from the tray menu **Выход**, or from the **Выход** button in the window.
5. The **Автозапуск при входе** check is in the window and in the tray menu. It writes `~/.config/autostart/ritm.desktop`. Both GNOME and KDE read that file.

The tray icon appears when PyGObject is installed:

```text
sudo apt install python3-gi gir1.2-ayatanaappindicator3-0.1
```

Without those packages there is no tray icon, but the window, counters, sound, and notifications still work. Notifications use `notify-send` (GNOME and KDE). The sound is the system `dialog-warning`.

Starting the program again does not create a second copy. It opens the window.
=======
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
>>>>>>> 9492aa463bd5615b5f2569ed2729a16f3ca0027b
