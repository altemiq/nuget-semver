SET version=10.0.3

CALL test.cmd
CALL release.cmd %version%
CALL pack.cmd %version%