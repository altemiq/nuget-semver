SET version=10.0.2

CALL test.cmd
CALL release.cmd %version%
CALL pack.cmd %version%