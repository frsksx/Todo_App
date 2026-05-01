# Diagnostics

## Test Entry Point

From the repository root:

```powershell
.\tests\run-tests.ps1
```

Or, on a POSIX shell:

```bash
./tests/run-tests.sh
```

Both wrappers run `dotnet test Todo-App.sln`, write normal console output, and regenerate:

```text
tests/_results/test-results*.trx
```

On Windows machines that block direct `.ps1` execution, use:

```cmd
tests\run-tests.cmd
```

## Investigating Failures

1. Re-run the full suite and note the failing test names.
2. Open the relevant `tests/_results/test-results*.trx` file and inspect failed `<UnitTestResult>` entries.
3. Re-run a single test with:

```powershell
.\tests\run-tests.ps1 "FullyQualifiedName~TestName"
```

4. For database integration failures, look for the temp database path in the failing test output. Temp DBs are created through `Database.CreateTemp()` and normally removed on dispose.

## Determinism Rules

- Domain and infrastructure tests should use `FakeClock` and `SequentialIdGenerator`.
- Tests should avoid direct `DateTime.UtcNow`, `Guid.NewGuid()`, random values, and sleeps.
- UI smoke tests are intentionally not part of the default suite.
