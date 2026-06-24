SET version=10.0.9

CALL test.cmd
CALL release.cmd %version%
CALL pack.cmd %version%