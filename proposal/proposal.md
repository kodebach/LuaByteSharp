# LuaByteSharp

## Kurzbeschreibung des Projekts
LuaByteSharp soll ein Interpreter für Lua Byte-Code werden. Der Interpreter soll in C# geschrieben werden.
Das Programm soll fähig sein, den vom Lua Standardcompiler luac, in der Version 5.3, erzeugten Byte-Code einzulesen und auszuführen.
Geplant ist, dass alle der 47 existierenden Operationen ([siehe](https://www.lua.org/source/5.3/lopcodes.h.html)) unterstützt werden.
Als Definition der Funktionsweise dieser Operationen werden die Beschreibungen im luac Sourcefile [`lopcodes.h`](https://www.lua.org/source/5.3/lopcodes.h.html),
sowie die ausführlicheren Beschreibungen auf [dieser](https://github.com/dibyendumajumdar/ravi/blob/master/readthedocs/lua_bytecode_reference.rst) Github Seite verwendet.
Geplant ist ebenfalls, dass das Metatable-Konzept ([siehe](https://www.lua.org/manual/5.3/manual.html#2.4)) von Lua unterstützt wird.

Da Lua-Code oft auf der Lua Standard Library aufbaut, soll zumindest ein Subset der Standard Library unterstützt werden.
Das Subset wird wahrscheinlich je nach verfügbarer Zeit angepasst werden. Jedenfalls werden aber weder `require` noch `load` bzw. `loadfile` und infolgedessen auch nicht `dofile` untersützt werden, da alle diese Funktionen Lua Sourcecode laden können, der Interpreter diesen aber nicht parsen kann.

## Detailierte Auflistung der Bestandteile

### Byte Code Instruktionen
siehe [https://www.lua.org/source/5.3/lopcodes.h.html](https://www.lua.org/source/5.3/lopcodes.h.html)
bzw. [https://github.com/dibyendumajumdar/ravi/blob/master/readthedocs/lua_bytecode_reference.rst](https://github.com/dibyendumajumdar/ravi/blob/master/readthedocs/lua_bytecode_reference.rst)

### Standard Library Funktionen
[siehe auch](https://www.lua.org/manual/5.3/manual.html#6)
#### Basic Functions
Bis auf die folgenende Ausnahmen, sollen alle Basic Functions unterstützt werden:
* `collectgarbage`
* `dofile`
* `load`
* `loadfile`
* `pcall`
* `xpcall`

#### String Manipulation
Auch hier ist geplant, bis auf einige Ausnahmen, alle Funktionen zu unterstützen.

Ausnahmen:
* `string.dump`
* `string.pack`
* `string.packsize`
* `string.unpack`

#### UTF-8 Support
Die folgenden Methoden sollen alle unterstützt werden:
* `utf8.char`
* `utf8.charpattern`
* `utf8.codes`
* `utf8.codepoint`
* `utf8.len`
* `utf8.offset`

#### Table Manipulation
Die folgenden Methoden sollen alle unterstützt werden:
* `table.concat`
* `table.insert`
* `table.move`
* `table.pack`
* `table.remove`
* `table.sort`
* `table.unpack`

#### Mathematical Functions
Alle mathematischen Funktionen der Lua Standard Library sollen unterstützt werden.

### Mögliche Erweiterungen
Möglicherweise werden auch alle oder Teile der IO Funktionen unterstützt werden.
Auch die Funktionen `string.pack`, `string.packsize` und `string.unpack` werden vielleicht unterstützt.

## Aufwandsabschätzungen

| Komponente                | Aufwand   |
|---                        |---        |
| Byte Code Parser          |           |
| Interpreter Grundgerüst   |           |
| Byte Code Instruktionen   |           |
| Standard Library          |           |

### Byte Code Instruktionen

| Gruppe            | Instruktionen                                                     | Aufwand   |
|---                |---                                                                |---        |
| Move/Load         | `MOVE`, `LOADK`, `LOADKX`, `LOADBOOL`, `LOADNIL`                  |           |
| Up-Value/Table    | `(GET/SET)UPVAL`, `(GET/SET)TABUP`, `(GET/SET)TABLE`, `NEWTABLE`  |           |
| Arithmetik        | `ADD`, `SUB`, `MUL`, `DIV`, `IDIV`, `MOD`, `POW`, `UNM`           |           |
| Bit Manipulation  | `BAND`, `BOR`, `BXOR`, `SHL`, `SHR`, `BNOT`                       |           |
| Logik             | `TEST`, `TESTSET`, `NOT`                                          |           |
| Vergleiche        | `EQ`, `LT`, `LE`                                                  |           |
| Schleifen         | `FORLOOP`, `FORPREP`, `TFORLOOP`, `TFORCALL`                      |           |
| Funktionen        | `CALL`, `TAILCALL`, `RETURN`, `CLOSURE`                           |           |
| Andere            | `JMP`, `VARARG`, `SETLIST`, `CONCAT`, `LEN`, `SELF`, `EXTRAARG`   |           |


### Standard Library Funktionen

| Gruppe                | Funktionen                                                                                    | Aufwand   |
|---                    |---                                                                                            |---        |
| Basis Funktionen      | `assert`, `error`, `_G`, `getmetatable`, `ipairs`, `next`, `pairs`, `print`, `rawequal`, `rawget`, `rawlen`, `rawset`, `select`, `tonumber`, `tostring`, `type`, `_VERSION` |           |
| String Manipulation   | `string.byte`, `string.char`, `string.find`, `string.format`, `string.gmatch`, `string.gsub`, `string.len`, `string.lower`, `string. match`, `string.rep`, `string.reverse`, `string.sub`, `string.upper` |           |
| Mathematik            | `math.abs`, `math.acos`, `math.asin`, `math.atan`, `math.ceil`, `math.cos`, `math.deg`, `math.exp`, `math.floor`, `math.fmod`, `math.huge`, `math.log`, `math.max`, `math.maxinteger`, `math.min`, `math.mininteger`, `math.modf`, `math.pi`, `math.rad`, `math.random`, `math.randomseed`, `math.sin`, `math.sqrt`, `math.tan`, `math.tointeger`, `math.type`, `math.ult` |           |
| Table Manipulation    | `table.concat`, `table.insert`, `table.move`, `table.pack`, `table.remove`, `table.pack`, `table.remove`, `table.sort`, `table.unpack` |           |
| UTF-8 Support         | `utf8.char`, `utf8.charpattern`, `utf8.codes`, `utf8.codepoint`, `utf8.len`, `utf8.offset`    |           | 