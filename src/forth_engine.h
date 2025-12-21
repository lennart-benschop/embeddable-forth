/* Embeddable C-based FORTH interpreter.
   Copyright 2025 L.C. Benschop, Vught, The Netherlands.
   The program is released under the MIT license.
   There is NO WARRANTY.
*/


struct engine_state {
  uint8_t *dict;
  uint32_t dict_size;
  uint8_t *ip;
  uint32_t *sp;
  uint32_t *rp;
  uint64_t *lp;
  double *fp;
};

void set_irq(uint32_t flags);

#define ENGINE_EXIT_IRQ   0x1
#define ENGINE_DIVIDE_IRQ 0x2
#define ENGINE_BREAK_IRQ  0x4
#define ENGINE_ALARM_IRQ  0x8

void forth_io(uint8_t opcode, struct engine_state *state);

void forth_io_init(int argc, char **argv);
void forth_io_exit(void);

int
load_engine_from_mem(struct engine_state* state, const uint32_t* dict);
		   
int
load_engine_from_file(struct engine_state* state, char *fname);

int
run_engine(struct engine_state* state);

int
remove_engine(struct engine_state* state);

#define FORTH_MAGIC 0x54524F46

struct dict_header {
  uint32_t magic;            // Magic number (0x54524F46) 
  uint32_t min_vm_level;     // Minimum VM level. 0x010000
  uint32_t req_vm_featuress; // VM features used by this dicionary.
  uint32_t img_size;  // Size in bytes of image stored in file or memory.
  uint32_t dict_size; // Total size of FORTH dictionary area in bytes.
  uint32_t entry_point;  // Initial instruction pointer (byte offset to start).
  uint32_t sp0;       // Initial stack pointer.
  uint32_t rp0;       // initial return stack pointer  
  uint32_t lp0;       // initial local stack pointer
  uint32_t fp0;       // initial floating point stack pointer
  uint32_t div_ex;    // Address of integer division exception handler.
  uint32_t brk_ex;    // Address of break key handler.
  uint32_t tim_ex;    // Address of timer interrupt handler.
  uint32_t seg_ex;    // Address of segmentation failt interrupt handler.
};

/* Memory map
   dict_header (including interrupt vector addresses).
   Forth dictionary entries
 -> HERE == img_size
   PAD + free area
   stack expands into free area.
   sp0
   return stack (32-bit aligned).
   rp0
   locals stack (64-bit aligned)
   lp0
   floating stack (64-bit alinged)
   fp0
   dict size
 */
