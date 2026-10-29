package store

import (
	"crypto/rand"
	"encoding/binary"
	"fmt"
	"time"
)

// NewUUID 生成一个 RFC 4122 风格的 UUID v4 字符串。
// 自己实现是为了避免为了一个 ID 引入额外依赖。
func NewUUID() string {
	var b [16]byte
	if _, err := rand.Read(b[:]); err != nil {
		// crypto/rand 失败属于极端情况，用纳秒时间戳兜底，保证服务不崩。
		ns := uint64(time.Now().UnixNano())
		binary.BigEndian.PutUint64(b[:8], ns)
		binary.BigEndian.PutUint64(b[8:], ns^0x9e3779b97f4a7c15)
	}
	b[6] = (b[6] & 0x0f) | 0x40 // version 4
	b[8] = (b[8] & 0x3f) | 0x80 // variant 10
	return fmt.Sprintf("%x-%x-%x-%x-%x", b[0:4], b[4:6], b[6:8], b[8:10], b[10:16])
}
