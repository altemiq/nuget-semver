SET version=10.0.0

CALL test.cmd
CALL release.cmd %version%
CALL pack.cmd %version%