/* Embeddable C-based FORTH interpreter.
   Copyright 2025 L.C. Benschop, Vught, The Netherlands.
   The program is released under the MIT license.
   There is NO WARRANTY.
*/

#include <stdint.h>
#include <string.h>
#include <stdlib.h>
#include <stdio.h>
#include <math.h>

#include "forth_engine.h"
#include "forth_opcodes.h"

static int
setup_engine(struct engine_state* state)
{
  struct dict_header *hdr = (struct dict_header*)state->dict;
  state->ip = state->dict + hdr->entry_point;
  state->sp = (uint32_t *)(state->dict + hdr->sp0);
  state->rp = (uint32_t *)(state->dict + hdr->rp0);
  state->lp = (uint64_t *)(state->dict + hdr->lp0);
  state->fp = (double *)(state->dict + hdr->fp0);
  return 0;
}

static volatile int32_t int_flags;

void set_irq(uint32_t flags)
{
  int_flags |= flags;
}

static int
alloc_engine(struct engine_state* state)
{
  state->dict = malloc(state->dict_size);
  if (state->dict == NULL)
    return -1;
  memset(state->dict, 0, state->dict_size);
  return 0;
}



int
load_engine_from_mem(struct engine_state* state, const uint32_t* dict)
{
  struct dict_header *hdr = (struct dict_header*)dict;
  if (hdr->magic != FORTH_MAGIC)
    return -2;
  state->dict_size = hdr->dict_size;
  if (alloc_engine(state))
    return -1;
  memcpy(state->dict, dict, hdr->img_size);
  setup_engine(state);
  return 0;
}
		   
int
load_engine_from_file(struct engine_state* state, char *fname)
{
  struct dict_header hdr;
  FILE *img = fopen(fname, "rb");
  if (!img) {
    return -4;
  }
  if (fread(&hdr, sizeof(hdr), 1, img) < 1) {
    fclose(img);
    return -3;
  }
  if (hdr.magic != FORTH_MAGIC) {
    fclose(img);
    return -2;
  }
  state->dict_size = hdr.dict_size;
  if (alloc_engine(state)) {
    fclose(img);
    return -1;
  }
  memcpy(state->dict, &hdr, sizeof(hdr));
  if (fread(state->dict + sizeof(hdr), 1, hdr.img_size - sizeof(hdr), img) < 1) {
    remove_engine(state);
    fclose(img);
    return -3;
  }
  setup_engine(state);
  return 0;  
}

int
run_engine(struct engine_state* state )
{
#ifdef FORTH_ALLOW_UNALIGNED
#define GET_LIT16() (ip+=2,*(uint16_t*)(ip-2))
#define GET_LIT24() (ip+=3,*(uint32_t*)(ip-3)&0xffffff)
#define GET_LIT32() (ip+=4,*(uint32_t*)(ip-4))
#else  
#define GET_LIT16() (ip+=2,ip[-2] | (ip[-1]<<8))
#define GET_LIT24() (ip+=3,ip[-3] | (ip[-2]<<8) | (ip[-1]<<16))
#define GET_LIT32() (ip+=4,ip[-4] | (ip[-3]<<8) | (ip[-2]<<16) |((uint32_t)ip[-1]<<24))
#endif  
  uint8_t *dict_base = state->dict;
  uint8_t *ip = state->ip;
  uint32_t *sp = state->sp;
  uint32_t *rp = state->rp;
  uint64_t *lp = state->lp;
  double *fp = state->fp;
  uint32_t tos = *sp++;
  uint32_t t1,t2;
  int_flags = 0;
  for (;;) {
    uint8_t opc = *ip++;
    //printf("opcode %02x ip=%08x sp=%08x rp=%08x tos=%08x s0=%08x s1=%08x\n",opc,
    //	   ip-dict_base, (uint8_t*)sp-dict_base,(uint8_t*)rp-dict_base,tos,sp[0],sp[1]);
    switch(opc) {
    case OP_NOOP: 
      break;
    case OP_CALL: /* Call with 24-bit address */
      t1 = GET_LIT24();
      *--rp = ip-dict_base;
      ip = dict_base + t1;
      break;
    case OP_FBRANCH: /* forward branch 16 bit displacement*/
      t1 = GET_LIT16();
      ip += t1;
      break;
    case OP_BBRANCH: /* backward branch 16 bit displacement*/
      t1 = GET_LIT16();
      ip -= t1;
      break;
    case OP_QFBRANCH: /* forward conditional branch 16 bit displacement*/
      t1 = GET_LIT16();
      if (tos==0)
	ip += t1;
      tos = *sp++;
      break;
    case OP_QBBRANCH: /* backward conditional branch 16 bit displacement*/
      t1 = GET_LIT16();
      if (tos==0)
	ip -= t1;
      tos = *sp++;
      break;
    case OP_LIT8: /* 8-bit literal */
      *--sp = tos;
      tos = *ip++;
      break;
    case OP_LIT8N: /* 8-bit negative literal */
      *--sp = tos;
      tos = -*ip++;
      break;
    case OP_LIT16: /* 16-bit literal */
      *--sp = tos;
      tos = GET_LIT16();
      break;
    case OP_LIT16N: /* 16-bit negative literal */
      *--sp = tos;
      tos = -GET_LIT16();
      break;
    case OP_LIT24: /* 24-bit literal */
      *--sp = tos;
      tos = GET_LIT24();
      break;
    case OP_LIT24N: /* 24-bit negative literal */
      *--sp = tos;
      tos = -GET_LIT24();
      break;
    case OP_LIT: /* 32-bit literal */
      *--sp = tos;
      tos = GET_LIT32();
      break;
    case OP_FLIT: /* Floating point literal */
      {
	fp--;
	uint64_t *td= (uint64_t*)fp;
	t1 = GET_LIT32();
	t2 = GET_LIT32();
	*td = t1 | ((uint64_t)t2 << 32);
      }
      break;
    case OP_QDO: /* Runtime part of ?DO */
      t1 = *sp++;
      t2 = GET_LIT16();
      if (t1 == tos) {
	ip += t2;
      } else {
	*--rp = t1;
	*--rp = tos;	
      }
      tos = *sp++;
      break;
    case OP_DO: /* Runtime part of DO */
      t1 = *sp++;
      *--rp = t1;
      *--rp = tos;
      tos = *sp++;
      break;
    case OP_LOOP: /* Runtime part of LOOP */
      t2 = rp[0]+1;
      t1 = GET_LIT16();
      if (t2 == rp[1]) {
	rp += 2;
      } else {
	rp[0] = t2;
	ip -= t1;
      }
      break;
    case OP_PLUSLOOP: /* Runtime part of +LOOP */
      t2 = rp[0] + tos;
      t1 = GET_LIT16();
      if (((int32_t)(t2 - rp[1]) < 0) != ((int32_t)(rp[0] - rp[1]) < 0)) {
	rp += 2;
      } else {
	rp[0] = t2;
	ip -= t1;
      }
      tos = *sp++;
      break;
    case OP_LEAVE: /* Runtime part of LEAVE */
      t1 = GET_LIT16();
      ip += t1;
      rp += 2;
      break;
    case OP_EXIT: 
      ip = dict_base + *rp++;
      break;
    case OP_RTI: /* Return from interrupt, same as EXIT for now */
      ip = dict_base + *rp++;
      break;
    case OP_RFROM: /* R> */
      *--sp = tos;
      tos = *rp++;
      break;
    case OP_TOR: /* >R */
      *--rp = tos;
      tos = *sp++;
      break;
    case OP_RFETCH: /* R@ */           
      *--sp = tos;
      tos = *rp;
      break;
    case OP_I:
      *--sp = tos;
      tos = *rp;
      break;
    case OP_IPRIME: /* I' */
      *--sp = tos;
      tos = rp[1];
      break;
    case OP_J:
      *--sp = tos;
      tos = rp[2];
      break;
    case OP_UNLOOP:            
      rp+=2;
      break;
    case OP_DROP:
      tos = *sp++;
      break;
    case OP_DUP:
      *--sp = tos;
      break;
    case OP_SWAP:
      t1 = *sp;
      *sp = tos;
      tos = t1;
      break;
    case OP_OVER:
      *--sp = tos;
      tos = sp[1];
      break;
    case OP_NIP:
      sp++;
      break;
    case OP_TUCK:
      t1 = *sp;
      *sp = tos;
      *--sp = t1;
      break;
    case OP_ROT:
      t1 = *sp;
      t2 = sp[1];
      *sp = tos;
      tos = t2;
      sp[1] = t1;
      break;
    case OP_ROTN: /* -ROT */
      t1 = *sp;
      t2 = sp[1];
      sp[1] = tos;
      tos = t1;
      *sp = t2;
      break;
    case OP_2DROP:
      sp++;
      tos = *sp++;
      break;
    case OP_2DUP:
      t1 = *sp;
      *--sp = tos;
      *--sp = t1;
      break;
    case OP_2SWAP:
      t1 = sp[1];
      sp[1] = tos;
      tos = t1;
      t1 = sp[2];
      sp[2] = *sp;
      *sp = t1;
      break;
    case OP_2OVER:
      *--sp = tos;
      t1 = sp[3];
      tos = sp[2];
      *--sp = t1;
      break;
    case OP_PICK:
      tos = sp[tos];
      break;
    case OP_ROLL:              
      t1 = sp[tos];
      for (t2=tos; t2>0; t2--) {
	sp[t2]=sp[t2-1];
      }
      sp++;
      tos = t1;
      break;
    case OP_2TIMES: /* 2* */
      tos = tos << 1;
      break;
    case OP_2SLASH: /* 2/ */
      tos = (int32_t)tos >> 1;
      break;
    case OP_PLUS:  /* + */
      t1 = *sp++;
      tos = t1 + tos;
      break;
    case OP_MINUS: /* - */            
      t1 = *sp++;
      tos = t1 - tos;
      break;
    case OP_AND:               
      t1 = *sp++;
      tos = t1 & tos;
      break;
    case OP_OR:                
      t1 = *sp++;
      tos = t1 | tos;
      break;
    case OP_XOR:               
      t1 = *sp++;
      tos = t1 ^ tos;
      break;
    case OP_NEGATE:
      tos = -tos;
      break;
    case OP_INVERT:            
      tos = ~tos;
      break;
    case OP_ABS:
      //tos = abs(tos); // UB checker in zig cc trips on it when tos=-$80000000
      if (tos & 0x80000000) tos=-tos;
      break;
    case OP_TIMES: /* * */            
      t1 = *sp++;
      tos = t1 * tos;
      break;
    case OP_UMTIMES: /* UM* */
      {
	t1 = *sp;
	uint64_t dt = (uint64_t)t1 * (uint64_t)tos;
	*sp = dt;
	tos = dt >> 32;
      }
      break;
    case OP_MTIMES:  /* M* */          
      {
	t1 = *sp;
	int64_t dt = (int64_t)(int32_t)t1 * (int64_t)(int32_t)tos;
	*sp = dt;
	tos = dt >> 32;
      }
      break;
    case OP_USLASHMOD: /* U/MOD */
      {
	uint64_t dt = ((uint64_t)sp[0] << 32) | ((uint64_t)sp[1]);
	sp++;
	if (sp[-1] >= tos) {
	  sp[0] = -1;
	  tos = -1;	       
	} else {
	  sp[0] = dt % tos;
	  tos = dt / tos;
	}
      }
      break;
    case OP_FMSLASHMOD: /* FM/MOD */       
      {
	int64_t dt = ((uint64_t)sp[0] << 32) | sp[1];
	sp++;
	if (tos == 0 || (dt==0x8000000000000000LL && tos==-1)) {
	  sp[0] = -1;
	  tos = -1;	       
	} else {
	  sp[0] = dt % (int32_t)tos;
	  t1 = dt / (int32_t)tos;
	  if (((sp[0] ^ tos) & 0x80000000) && sp[0]!=0) {
	    sp[0]+=tos;
	    t1--;
	  }
	  tos = t1;
	}
      }
      break;
    case OP_SMSLASHREM: /* SM/REM */       
      {
	int64_t dt = ((uint64_t)sp[0] << 32) | sp[1];
	sp++;
	if (tos == 0 || (dt==0x8000000000000000LL && tos==-1)) {
	  sp[0] = -1;
	  tos = -1;	       
	} else {
	  sp[0] = dt % (int32_t)tos;
	  tos = dt / (int32_t)tos;
	}
      }
      break;
    case OP_SLASH: /* / */
      t1 = *sp++;
      if (tos==0 || (tos==-1 && t1==0x80000000)) {
	tos = -1;
      } else {
	t2 = (int32_t)t1 / (int32_t)tos;
	if (((t1^tos) & 0x80000000) && (tos*t2!=t1)) t2--;
	tos = t2;
      }
      break;
    case OP_MOD:               
      t1 = *sp++;
      if (tos==0 || (tos==-1 && t1==0x80000000)) {
	tos = -1;
      } else {
	t2 = (int32_t)t1 % (int32_t)tos;
	if (((t2^tos) & 0x80000000) && (t2 != 0)) t2+=tos;
	tos = t2;
      }
      break;
    case OP_DPLUS: /* D+ */
      {
	uint64_t dt1 = ((uint64_t)tos << 32) | *sp++;
	uint64_t dt2 = ((uint64_t)sp[0]<<32) | sp[1];
	sp++;
	dt2 = dt2 + dt1;
	tos = dt2>>32;
	sp[0] = dt2;
      }
      break;			
    case OP_DNEGATE:           
      {
	uint64_t dt1 = ((uint64_t)tos << 32) | sp[0];
	dt1 = -dt1;
	tos = dt1>>32;
	sp[0] = dt1;
      }
      break;			
    case OP_DMINUS: /* D- */          
      {
	uint64_t dt1 = ((uint64_t)tos << 32) | *sp++;
	uint64_t dt2 = ((uint64_t)sp[0]<<32) | sp[1];
	sp++;
	dt2 = dt2 - dt1;
	tos = dt2>>32;
	sp[0] = dt2;
      }
      break;			
    case OP_DTIMES: /* D* */           
      {
	uint64_t dt1 = ((uint64_t)tos << 32) | *sp++;
	uint64_t dt2 = ((uint64_t)sp[0]<<32) | sp[1];
	sp++;
	dt2 = dt2 * dt1;
	tos = dt2>>32;
	sp[0] = dt2;
      }
      break;			
    case OP_UDSLASHMOD: /* UD/MOD */       
      {
	uint64_t dt1 = ((uint64_t)tos << 32) | sp[0];
	uint64_t dt2 = ((uint64_t)sp[1]<<32) | sp[2];
	uint64_t dt3;
	if (dt1 == 0) {
	  dt3 = -1LL;
	  dt2 = -1LL;
	} else {
	  dt3 = dt2 / dt1;
	  dt2 = dt2 % dt1;
	}
	sp[2] = dt2;
	sp[1] = dt2>>32;
	tos = dt3>>32;
	sp[0] = dt3;
      }
      break;			
    case OP_DSLAHSMOD:  /* D/MOD */     
      {
	int64_t dt1 = ((uint64_t)tos << 32) | sp[0];
	int64_t dt2 = ((uint64_t)sp[1]<<32) | sp[2];
	int64_t dt3;
	if (dt1 == 0 || (dt1 == -1LL && dt2==0x8000000000000000LL)) {
	  dt3 = -1LL;
	  dt2 = -1LL;
	} else {
	  dt3 = dt2 / dt1;
	  dt2 = dt2 % dt1;
	  if (((dt2 ^ dt1) & 0x8000000000000000LL) && dt2 != 0) {
	    dt3 -= 1;
	    dt2 += dt1;
	  }
	}
	sp[2] = dt2;
	sp[1] = dt2>>32;
	tos = dt3>>32;
	sp[0] = dt3;
      }
      break;			
    case OP_LSHIFT:
      t1 = *sp++;
      tos = t1 << tos;
      break;
    case OP_RSHIFT:
      t1 = *sp++;
      tos = t1 >> tos;
      break;
    case OP_0EQ:   /* 0= */
      tos = -(tos == 0);
      break;
    case OP_0NEQ:  /* 0<> */
      tos = -(tos != 0);
      break;
    case OP_0LESS: /* 0< */
      tos = -((int32_t)tos < 0);
      break;
    case OP_0GREATER: /* 0> */
      tos = -((int32_t)tos > 0);
      break;
    case OP_0LESSEQ: /* 0<= */
      tos = -((int32_t)tos <= 0);
      break;
    case OP_0GREATEREQ: /* 0>= */
      tos = -((int32_t)tos >= 0);
      break;
    case OP_D2TIMES: /* D2* */
      {
	uint64_t dt = ((uint64_t)tos<<32) | *sp;
	dt <<= 1;
	tos = dt >> 32;
	*sp = dt;
      }
      break;
    case OP_D2SLASH: /* D2/ */
      {
	int64_t dt = ((uint64_t)tos<<32) | *sp;
	dt >>= 1;
	tos = dt >> 32;
	*sp = dt;
      }
      break;
    case OP_EQ:  /* = */
      t1 =  *sp++;
      tos = -(t1 == tos);
      break;
    case OP_NEQ: /* <> */
      t1 =  *sp++;
      tos = -(t1 != tos);
      break;
    case OP_LESS: /* < */
      t1 =  *sp++;
      tos = -((int32_t)t1 < (int32_t)tos);
      break;
    case OP_GREATER: /* > */
      t1 =  *sp++;
      tos = -((int32_t)t1 > (int32_t)tos);
      break;
    case OP_LESSEQ: /* <= */
      t1 =  *sp++;
      tos = -((int32_t)t1 <= (int32_t)tos);
      break;
    case OP_GREATEREQ: /* >= */
      t1 =  *sp++;
      tos = -((int32_t)t1 >= (int32_t)tos);
      break;
    case OP_1PLUS: /* 1+ */
      tos++;
      break;
    case OP_1MINUS: /* 1- */
      tos--;
      break;
    case OP_ULESS: /* U< */
      t1 =  *sp++;
      tos = -(t1 < tos);
      break;
    case OP_UGREATER: /* U> */
      t1 =  *sp++;
      tos = -(t1 > tos);
      break;
    case OP_ULESEQ: /* U<= */
      t1 =  *sp++;
      tos = -(t1 <= tos);
      break;
    case OP_UGREATEREQ: /* U>= */
      t1 =  *sp++;
      tos = -(t1 >= tos);
      break;
    case OP_WITHIN:
      t1 = *sp++;
      t2 = *sp++;
      tos = -(t2 >= t1 && t2 < tos);
      break;
    case OP_CELLPLUS: /* CELL+ */
      tos += 4;
      break;
    case OP_CELLMINUS: /* CELL- */
      tos -= 4;
      break;
    case OP_CELLS:
      tos <<= 2;
      break;
    case OP_CFETCH: /* C@ */
      tos = *(dict_base + tos);
      break;
    case OP_CSTORE: /* C! */
      t1 = *sp++;
      *(dict_base + tos) = t1;
      tos = *sp++;
      break;
    case OP_HFETCH: /* H@ fetch 16-bit word */
      tos = *(uint16_t*)(dict_base + tos);
      break;
    case OP_HSTORE:  /* H! store 16-bit word */
      t1 = *sp++;
      *(uint16_t*)(dict_base + tos) = t1;
      tos = *sp++;
      break;
    case OP_FETCH: /* @ */
      tos = *(int32_t*)(dict_base + tos);
      break;
    case OP_STORE: /* ! */
      t1 = *sp++;
      *(uint32_t *)(dict_base + tos) = t1;
      tos = *sp++;
      break;
    case OP_2FETCH: /* 2@ */
      *--sp = *(uint32_t*)(dict_base + tos + 4);
      tos = *(uint32_t*)(dict_base + tos);
      break;
    case OP_2STORE: /* 2! */
      t1 = *sp++;
      t2 = *sp++;
      *(uint32_t*)(dict_base + tos) = t1;
      *(uint32_t*)(dict_base + tos + 4) = t2;
      tos = *sp++;
      break;
    case OP_PCFETCH: /* PC@ fetch at absolute address (not relative to dict)*/
      tos = *(uint8_t*)(uintptr_t)tos;
      break;
    case OP_PCSTORE: /* PC! */
      t1 = *sp++;
      *(uint8_t*)(uintptr_t)tos = t1;
      tos = *sp++;
      break;
    case OP_PFETCH: /* P@ */
      tos = *(uint32_t*)(uintptr_t)tos;
      break;
    case OP_PSTORE: /* P! */
      t1 = *sp++;
      *(uint32_t*)(uintptr_t)tos = t1;
      tos = *sp++;
      break;
    case OP_CMOVE:
      t1 = *sp++;
      t2 = *sp++;
      for (uint32_t i=0; i<tos; i++) {
	*(dict_base+t1+i) = *(dict_base+t2+i);
      }
      tos = *sp++;
      break;
    case OP_CMOVEN: /* CMOVE> */
      t1 = *sp++;
      t2 = *sp++;
      for (uint32_t i=tos; i>0; i--) {
	*(dict_base+t1+i-1) = *(dict_base+t2+i-1);
      }
      tos = *sp++;
      break;
    case OP_FILL:
      t1 = *sp++;
      t2 = *sp++;
      if (t1) {
	memset(dict_base+t2, tos, t1);
      }
      tos = *sp++;
      break;
    case OP_ALIGNED:
      tos = (tos+3) & 0xfffffffc;
      break;
    case OP_COMPARE:
      { uint32_t t3 = *sp++;
	t2 = *sp++;
	t1 = *sp++;
	t1 = memcmp(dict_base+t1, dict_base+t3, tos<t2?tos:t2);
	if (t1 == 0) {
	  tos = t2 - tos;
	} else {
	  tos = t1;
	}
      }
      break;
    case OP_SCAN:
      {
	uint32_t i;
	t1= *sp++;
	t2= *sp;
	for ( i = 0; i<t1; i++) {
	  if (*(dict_base + t2 + i) == tos)
	    break;
	}
	*sp = t2 + i;
	tos = t1 - i;
      }
      break;
    case OP_SKIP:
      {
	uint32_t i;
	t1= *sp++;
	t2= *sp;
	for ( i = 0; i<t1; i++) {
	  if (*(dict_base + t2 + i) != tos)
	    break;
	}
	*sp = t2 + i;
	tos = t1 - i;
      }
      break;
    case OP_FIND: /* (FIND) c-addr u nfa --- cfa/c-addr f */
      t1 = *sp++;
      t2 = sp[0];
      while (tos != 0) {
	uint8_t *p = dict_base + tos;
	if ((*p & 0x1f) == t1) {
	  if (memcmp(dict_base+t2,p+1,t1) ==0) {
	    t2 = tos + 1 + t1;
	    t2 = (t2+3) & ~3;
	    t2 += 4;
	    *sp = t2;
	    if (*p & 0x40) tos = 1; else tos = -1;
	    goto found_it;
	  }
	}
	tos = *(uint32_t*)(dict_base+tos-4);
      }
      *sp = t2;
      tos = 0;
    found_it:
      break;
    case OP_HFETCHU: /* H@U unaligned */
      tos = *(dict_base+tos) | (*(dict_base+tos+1)<<8);
      break;
    case OP_HSTOREU: /* H!U unaligned */
      t1 = *sp++;
      *(dict_base+tos) = t1;
      *(dict_base+tos+1) = t1>>8;
      tos = *sp++;
      break;
    case OP_TFETCHU: /* T@U Fetch triple byte 24-bit unaligned*/
      tos = *(dict_base+tos) | (*(dict_base+tos+1)<<8) |
	(*(dict_base+tos+2)<<16);
      break;
    case OP_TSTOREU: /* T!U Fstore triple byte 24-bit unaligned*/
      t1 = *sp++;
      *(dict_base+tos) = t1;
      *(dict_base+tos+1) = t1>>8;
      *(dict_base+tos+2) = t1>>16;
      tos = *sp++;
      break;
    case OP_FETCHU: /* @U unaligned */
      tos = *(dict_base+tos) | (*(dict_base+tos+1)<<8) |
	(*(dict_base+tos+2)<<16) | ((uint32_t)*(dict_base+tos+3)<<24);
      break;
    case OP_STOREU: /* !U unaligned */
      t1 = *sp++;
      *(dict_base+tos) = t1;
      *(dict_base+tos+1) = t1>>8;
      *(dict_base+tos+2) = t1>>16;
      *(dict_base+tos+3) = t1>>24;
      tos = *sp++;
      break;
    case OP_PLUSSTORE:
      t1 = *sp++;
      *(uint32_t*)(dict_base + tos) += t1;
      tos = *sp++;
      break;
    case OP_DEQ:
      {
      	uint64_t dt1 = ((uint64_t)tos << 32) | *sp++;
	uint64_t dt2 = ((uint64_t)sp[0]<<32) | sp[1];
	sp+=2;
	tos = -(dt1 == dt2);
      }
      break;
    case OP_DLESS:
      {
      	int64_t dt1 = ((uint64_t)tos << 32) | *sp++;
	int64_t dt2 = ((uint64_t)sp[0]<<32) | sp[1];
	sp+=2;
	tos = -(dt2 < dt1);
      }
      break;
    case OP_DULESS:
      {
      	uint64_t dt1 = ((uint64_t)tos << 32) | *sp++;
	uint64_t dt2 = ((uint64_t)sp[0]<<32) | sp[1];
	sp+=2;
	tos = -(dt2 < dt1);
      }
      break;
    case OP_0:
      *--sp = tos;
      tos = 0;
      break;
    case OP_1:
      *--sp = tos;
      tos = 1;
      break;
    case OP_2:
      *--sp = tos;
      tos = 2;
      break;
    case OP_3:
      *--sp = tos;
      tos = 3;
      break;
    case OP_4:
      *--sp = tos;
      tos = 4;
      break;
    case OP_1N: /* -1 constant */
      *--sp = tos;
      tos = -1;
      break;
    case OP_TOLOC: /* >LOC push onto locals stack */
      *--lp = tos;
      tos = *sp++;
      break;
    case OP_TODLOC: /* >DLOC push double-precision onto locals stack */
      {
	t1 = *sp++;
	uint64_t d = ((uint64_t)tos << 32) | t1;
	*--lp = d;
	tos = *sp++;
      }
      break;
    case OP_TOFLOC: /* >FLOC push float onto locals stack */
      lp--;
      *(double*)lp = *fp++;
      break;
    case OP_LPPLUS: /* LP+  remove constant 8b number of locals from locals stack */
      lp += *ip++;
      break;
    case OP_LOCFETCH: /* LOC@  Fetch 8b item from locals stack */
      *--sp = tos;
      tos = lp[*ip++];
      break;
    case OP_LOCSTORE: /* LOC! Store to 8b item in locals stack */
      lp[*ip++] = tos;
      tos = *sp++;
      break;
    case OP_DLOCFETCH: /* DLOC@ Fetch double from locals stack */
      {
	uint64_t d = lp[*ip++];
	*--sp=tos;
	*--sp=d;
	tos = d>>32;
      }
      break;
    case OP_DLOCSTORE: /* DLOC! store double to locals stack */
      {
	uint64_t d = ((uint64_t)tos << 32) | *sp++;
	lp[*ip++] = d;
	tos = *sp++;
      }
      break;
    case OP_FLOCFETCH: /* FLOC@ fetch float from locals stack */
      *--fp = *(double*)(lp + *ip++);
      break;
    case OP_FLOCSTORE: /* FLOC! store float to locals stack */
      *(double*)(lp + *ip++) = *fp++;
      break;
    case OP_FPLUS:   /* F+ */
      fp[1] = fp[1] + fp[0];
      fp++;
      break;
    case OP_FMINUS:  /* F- */
      fp[1] = fp[1] - fp[0];
      fp++;
      break;
    case OP_FTIMES:  /* F* */
      fp[1] = fp[1] * fp[0];
      fp++;
      break;
    case OP_FSLASH:  /* F/ */
      fp[1] = fp[1] / fp[0];
      fp++;
      break;
    case OP_FMOD: 
      fp[1] = fmod(fp[1], fp[0]);
      fp++;
      break;
    case OP_DTOF: /* D>F double-to-float */
      {
	t1 = *sp++;
	int64_t dt = ((uint64_t)tos << 32) | t1;
	*--fp = dt;
	tos = *sp++;
      }
      break;
    case OP_FTOD: /* F>D single-to-float */
      {
	*--sp = tos;
	int64_t dt = *fp++;
	t1 = dt;
	tos = dt >> 32;
	*--sp = t1;
      }
      break;
    case OP_FROUND:
      *fp = round(*fp);
      break;
    case OP_FTRUNC:
      *fp = trunc(*fp);
      break;
    case OP_FSCALE:
      *fp = ldexp(*fp,tos);
      tos = *sp++;
      break;
    case OP_STOF: /* S>F single-to-float */
      *--fp = (int32_t)tos;
      tos = *sp++;
      break;
    case OP_FTOS: /* F>S double-to-float */
      *sp++ = tos;
      tos = (int32_t)*fp++;
      break;
    case OP_FABS:
      *fp = fabs(*fp);
      break;
    case OP_FNEGATE:
      *fp = -*fp;
      break;
    case OP_FFUNC: /* Misc float functions */
      opc = *ip++;
      switch(opc) {
      case 0: /* FSQRT */
	*fp = sqrt(*fp);
	break;
      case 1: /* FSIN */
	*fp = sin(*fp);
	break;
      case 2: /* FCOS */
	*fp = cos(*fp);
	break;
      case 3: /* FTAN */
	*fp = tan(*fp);
	break;
      case 4: /* FASIN */
	*fp = asin(*fp);
	break;
      case 5: /* FACOS */
	*fp = acos(*fp);
	break;
      case 6: /* FATAN */
	*fp = atan(*fp);
	break;
      case 7: /* FTAN2 */
	fp[1] = atan2(fp[1],fp[0]);
	fp++;
	break;
      case 8: /* FLN */
	*fp = log(*fp);
	break;
      case 9: /* FLOG */
	*fp = log10(*fp);
	break;
      case 10: /* FEXP */
	*fp = exp(*fp);
	break;
      case 11: /* F** */
	fp[1] = pow(fp[1],fp[0]);
	fp++;
	break;
      default:
	/* unknown opcode */
	printf("Illegal FP func %08x\n",opc);
	int_flags |= ENGINE_EXIT_IRQ;
      }
      break;
    case OP_FDROP:
      fp++;
      break;
    case OP_FDUP:
      fp--;
      fp[0] = fp[1];
      break;
    case OP_FSWAP:
      {
	double ft = fp[1];
	fp[1] = fp[0];
	fp[0] = ft;
      }
      break;
    case OP_FOVER:
      fp--;
      fp[0] = fp[2];
      break;
    case OP_FROT:
      {
	double ft = fp[2];
	fp[2] = fp[1];
	fp[1] = fp[0];
	fp[0] = ft;
      }
      break;
    case OP_FFETCH: /* F@ double-precision */
      fp--;
      fp[0] = *(double*)(dict_base + tos);
      tos = *sp++;
      break;
    case OP_FSTORE: /* F! double-precision */
      *(double*)(dict_base + tos) = fp[0];
      fp++;
      tos = *sp++;
      break;
    case OP_SFFETCH: /* SF@ single-precision */
      fp--;
      fp[0] = *(float*)(dict_base + tos);
      tos = *sp++;
      break;
    case OP_SFSTORE: /* SF! single-precision */
      *(float*)(dict_base + tos) = fp[0];
      fp++;
      tos = *sp++;
      break;
    case OP_FFETCHU: /* F@U: unaligned */
      {
	uint64_t dt =0;
	for (t1=0; t1<8; t1++) {
	  dt <<=8;
	  dt |= *(dict_base+tos+7-t1);
	}
	fp--;
	*(uint64_t*)fp = dt;
      }
      tos = *sp++;
      break;
    case OP_FSTOREU: /* F!U: unaligned */
      {
	uint64_t dt = *(uint64_t*)fp;
	for (t1=0; t1<8; t1++) {
	  *(dict_base+tos+t1) = dt & 0xff;
	  dt >>= 8;
	}
	fp++;
      }
      tos=*sp++;
      break;
    case OP_FISNEG:
      *--sp = tos;
      tos = signbit(*fp) ? -1:0;
      fp++;
      break;
    case OP_FISINF:
      *--sp = tos;
      tos = (isinf(*fp)||isnan(*fp)) ? -1:0;
      fp++;
      break;
    case OP_F0EQ: /* F0= */
      *--sp = tos;
      tos = -(*fp++ == 0.0);
      break;
    case OP_F0NEQ: /* F0<> */
      *--sp = tos;
      tos = -(*fp++ != 0.0);
      break;
    case OP_F0LESS: /* F0> */
      *--sp = tos;
      tos = -(*fp++ < 0.0);
      break;
    case OP_F0GREATER: /* F0> */
      *--sp = tos;
      tos = -(*fp++ > 0.0);
      break;
    case OP_F0LESSEQ: /* F0<= */
      *--sp = tos;
      tos = -(*fp++ <= 0.0);
      break;
    case OP_F0GREATEREQ: /* F0>= */
      *--sp = tos;
      tos = -(*fp++ >= 0.0);
      break;
    case OP_FEQ: /* F= */
      *--sp = tos;
      tos = -(fp[1] == fp[0]);
      fp+=2;
      break;
    case OP_FNEQ: /* F<> */
      *--sp = tos;
      tos = -(fp[1] != fp[0]);
      fp+=2;
      break;
    case OP_FLESS: /* F< */
      *--sp = tos;
      tos = -(fp[1] < fp[0]);
      fp+=2;
      break;
    case OP_FGREATER: /* F> */
      *--sp = tos;
      tos = -(fp[1] > fp[0]);
      fp+=2;
      break;
    case OP_FLESSEQ: /* F<= */
      *--sp = tos;
      tos = -(fp[1] <= fp[0]);
      fp+=2;
      break;
    case OP_FGREATEREQ: /* F>= */
      *--sp = tos;
      tos = -(fp[1] >= fp[0]);
      fp+=2;
      break;
    case OP_FREXP:
      *--sp = tos;
      {
	int e;
	frexp(*fp++, &e);
	tos = e;
      }
      break;
    case OP_FLOOR:
      *fp = floor(*fp);
      break;
    case OP_OSCALL: /* Varius OS functions */
      opc=*ip++;
      *--sp = tos;
      state->sp = sp; /* Only save sp, other pointers not used by forth_io */   
      forth_io(opc, state);
      sp = state->sp;
      tos = *sp++;
      break;
    case OP_SPFETCH: /* SP@ */
      *--sp = tos;
      tos = (uint8_t*)sp - dict_base;
      break;
    case OP_SPSTORE: /* SP! */
      sp = (uint32_t*)(dict_base + tos);
      tos = *sp++;
      break;
    case OP_RPFETCH: /* RP@ */
      *--sp = tos;
      tos = (uint8_t*)rp - dict_base;
      break;
    case OP_RPSTORE: /* RP! */
      rp = (uint32_t*)(dict_base + tos);
      tos = *sp++;
      break;
    case OP_LPFETCH: /* LP@ locals stack pointer */
      *--sp = tos;
      tos = (uint8_t*)lp - dict_base;
      break;
    case OP_LPSTORE: /* LP! locals stack pointer */
      lp = (uint64_t*)(dict_base + tos);
      tos = *sp++;
      break;
    case OP_FPFETCH: /* FP@ floats stack pointer */
      *--sp = tos;
      tos = (uint8_t*)fp - dict_base;
      break;
    case OP_FPSTORE: /* FP@ floats stack pointer */
      fp = (double *)(dict_base + tos);
      tos = *sp++;
      break;
    default:
      /* unknown opcode */
      printf("Illegal opcode %08x\n",opc);
      int_flags |= ENGINE_EXIT_IRQ;
    }
    if(int_flags) {
      if (int_flags & ENGINE_EXIT_IRQ) {
	break;
      } else if (int_flags & ENGINE_DIVIDE_IRQ) {
	int_flags &= ~ENGINE_DIVIDE_IRQ;
	*--rp = ip-dict_base;
	ip = dict_base+*(uint32_t*)(dict_base + 0x28);
      } else if (int_flags & ENGINE_BREAK_IRQ) {
	int_flags &= ~ENGINE_BREAK_IRQ;
	*--rp = ip-dict_base;
	ip = dict_base+*(uint32_t*)(dict_base + 0x2C);
      } else if (int_flags & ENGINE_ALARM_IRQ) {
	int_flags &= ~ENGINE_ALARM_IRQ;
	*--rp = ip-dict_base;
	ip = dict_base+*(uint32_t*)(dict_base + 0x30);
      }
    }
  }
  *--sp = tos;
  state->ip = ip;
  state->sp = sp;
  state->rp = rp;
  state->lp = lp;
  state->fp = fp;
  return int_flags;
}

int
remove_engine(struct engine_state* state)
{
  if (state->dict) free(state->dict);
  memset(state, 0, sizeof(*state));
  return 0;
}
