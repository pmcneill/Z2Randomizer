﻿.macpack common

.export SwapPRG, SwapToSavedPRG, SwapToPRG0

FREE "PRG7" [$FEAA, $FFFC)

.segment "HEADER"
.org $6
.byte $52 ; Mapper 5
.byte $08 ; use NES 2.0 header

.org $a
.byte $70 ; reserve 8kb of PRG NVRAM (SRAM)

.segment "PRG7"

bank7_code0 = $c000

.export NmiBankShadow8, NmiBankShadowA
NmiBankShadow8 = $7b2
NmiBankShadowA = $7b3

.org $c4d0
    lda     #$44            ; vertical mirroring
    sta     $5105

.org $fffc
    .word (bank7_reset)

.reloc
bank7_reset:
    sei
    cld
    ldx #$00
    stx $2000
    inx
    stx $5103           ; Allow writing to WRAM
wait_ppu:
    lda $2002
    bpl wait_ppu
    dex
    beq wait_ppu
    txs
    stx $5117           ; Top bank is last bank
    dex
    stx $5116
    ldx #2
    stx $5102           ; Allow writing to WRAM
    inx
    stx $5100           ; mode 3 is 4 8kb PRG banks
    stx $5101           ; mode 3 is CHR mode 1x8k banks
    lda #$50            ; horizontal mirroring
    sta $5105
    ; Set the last sprite bank to a fixed bank for items
    lda #$68
    sta $5127
    jsr SwapCHR
    lda #$07
    jsr SwapPRG
    sta $5113           ; Explicitly switching to PRG RAM bank 0 works around a bug in nintendulator
    jmp bank7_code0


.reloc
SwapCHR:
    ; 1kb sprite banks are 20-27 and the 1kb bg banks are 28-2b
    asl
    asl
    sta $5120
    adc #1
    sta $5121
    adc #1
    sta $5122
    adc #1
    sta $5123
    adc #1
    sta $5124
    sta $5128
    adc #1
    sta $5125
    sta $5129
    adc #1
    sta $5126
    sta $512a
    adc #1
    ; sta $5127 ; Reserve the last sprite bank for new custom item tiles
    sta $512b
    lda #0
    rts

.reloc
SwapToPRG0:
    lda #$00
    beq SwapPRG
SwapToSavedPRG:
    lda $0769
SwapPRG:
    asl
    ora #$80
    sta NmiBankShadow8
    sta $5114
    clc
    adc #1
    sta NmiBankShadowA
    sta $5115
    lda #0
    rts


.segment "PRG7"
; Patch the save and exit command list to add a clear ram chunk
.org $FE80
    .word (ClearPartialDevRAM)

.reloc
ClearPartialDevRAM:
    ; Clear the stack ram we use for variables, but leave 16 untouched for things that
    ; should persist across resets. Because why not.
    lda #0
    ldx #$c0 - $20
@Loop2:
        sta $100 + $20 - 1,x
        dex
        bne @Loop2
    jmp $CF05

.segment "PRG0"
; Clean up stuff in bank zero - make it go via bank7's routines.
.org $8149
    lda     #$50            ; horizontal mirroring
    sta     $5105
.org $8150
    jsr     SwapCHR
.org $a86b
    jsr     SwapCHR

.segment "PRG5"
; Clean up stuff in bank 5 - make it go via bank7's routines.
.org $a712
    lda     #$50            ; horizontal mirroring
    sta     $5105
.org $a728
    jsr     SwapCHR

; Patch power off/ soft reset to clear out some stack ram for use
.org $a6a3
    jsr ClearStackRAM

.reloc
ClearStackRAM:
    lda #0
    ldx #$d0 - $20
    ; clear stack RAM we can use for variables
@Loop2:
        sta $100 + $20 - 1,x
        dex
        bne @Loop2
    jmp $d281 ; clear rest of ram

; Fix sword flashing on title screen bug

; Switch the OAM position of an unused sprite and the glitchy sword tile
; and move that just off to the left. This sprite just doesn't seem to glitch?

.org $A7C1 + 22 * 4
    .byte $80,$E8,$20,$70

.org $A7C1 + 26 * 4
    .byte $8F,$EA,$02,$78

; Star twinkle animation, twinkle one less so the sword doesn't twinkle.
.org $a8cf
    ldx #$6b - 4
.org $a8d7
    inc $0267 + 4,x
    lda $0267 + 4,x
    and #$23
    sta $0267 + 4,x

.segment "PRG7"
; Update the pointers to the bank switches

UPDATE_REFS SwapCHR @ $C342 $C3B9 $C633 $CA10 $D045 $D050
UPDATE_REFS SwapPRG @ $C00B $C035 $C1C4 $C33F $C38B $CD5D $CF24
UPDATE_REFS SwapToSavedPRG @ $C1DB $C24A $C250 $C4D6 $C63C $C645 $CB36 $CF2A $CF3D $CF50
UPDATE_REFS SwapToSavedPRG @ $CFFD $D127 $D502 $DFD3 $DFDC $DFF0 $DFF9 $E002 $E01C $E025 $E071 $E1DE
UPDATE_REFS SwapToPRG0 @ $C1CA $C1CE $C256 $C350 $C636 $C68B $C9EF $CCFA $D0FD $D121 $D3EA $D508
UPDATE_REFS SwapToPRG0 @ $DFD9 $DFE2 $DFF6 $DFFF $E017 $E022 $E02B $E077 $E1E4 $FF4A

.segment "PRG0"

; move the scrolling code that loads metatiles from the current area
; from the fixed bank to bank 0 (the only place that calls it)
; Normally we are only able to read the metatiles from the fixed bank
; since MMC1 is 16k / 16k banking but because we are on mmc5, we can
; and load the pointers into other banks that MMC1 couldn't
; This clears up about 200 bytes from the fixed bank ($feea, $ff70)

.org $9AFD
    jsr LoadAreaBGMetatile

.reloc
LoadAreaBGMetatile:
; if the code is in a000 or higher that would switch this bank out from under itself
.assert * > $a000
    lda $0769
    asl
    ora #$80
    sta NmiBankShadow8
    sta $5114
    ; Store the loop counter back into $01
@Loop:
        stx $01
        ; Load background tile id from the column buffer high bits
        ; Transform it as follows to get the attribute and metatile
        ; xx000000 -> attr and xx000000 -> 00000xx0 -> tileid
        lda $0464,x
        and #$c0
        sta $03
        asl a
        rol a
        rol a
        asl a
        tay
        ; Read from the fixed address for the backgrounds of the "current" banks
        lda $8500,y
        sta $0e
        lda $8501,y
        sta $0f
        ; Write 00xxxx00 as 0000xxxx into $02
        lda $0464,x
        and #$3f
        asl a
        asl a
        sta $02
        ; And then add 2 if 740 is even. This selects the left or right side of the metatile
        lda $0740
        and #$01
        eor #$01
        asl a
        adc $02
        ; Use as the offset from the pointer to read the actual tile from the metatile
        tay
        ldx $00
        lda ($0e),y
        sta $03a7,x
        iny
        lda ($0e),y
        sta $03a8,x
        ; $03 is the attribute in the format xx000000
        ; $04 is set to 0 before entering
        ldy $04
        ; $05 is set by the caller
        ; Branch if we are loading the attributes for the right side 
        lda $05
        bne @LoadRightAttribute
@LoadLeftAttribute:
        ; Every other attribute (when the loop counter $01 is odd)
        ; moves to the next byte in the attribute buffer
        lda $01
        lsr a
        bcs @BottomLeftAttribute
@TopLeftAttribute:
        rol $03
        rol $03
        rol $03
        jmp @WriteAttribute
@LoadRightAttribute:
        ; Every other attribute (when the loop counter $01 is odd)
        ; moves to the next byte in the attribute buffer
        lda $01
        lsr a
        bcs @BottomRightAttribute
@TopRightAttribute:
        lsr $03
        lsr $03
        lsr $03
        lsr $03
        jmp @WriteAttribute
@BottomLeftAttribute:
        lsr $03
        lsr $03
        ; fallthrough to move to the next attribute byte in the buffer
        ; The bottom right attribute doesn't need shifting
@BottomRightAttribute:
        inc $04
        ; Write attribute data stored in $03 to the attribute buffer
@WriteAttribute:
        lda $0471,y
        ora $03
        sta $0471,y
        inc $00
        inc $00
        ldx $01
        inx
        cpx #$0d
        bcc @Loop
    ; switch back to 0 just in case
    jmp SwapToPRG0


; Move sprites out of the last fixed bank
; Instead of using the blank tile sprites to cover the right 8px on the overworld, just use
; any other full color sprite (since the palette is all black this is fine)
.org $878c
    lda #$7f

; link's overworld sprite has an invisible half that needs moved outta the new sprite bank as well
.org $8739
    lda #$73
