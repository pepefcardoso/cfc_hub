"use client"

import * as React from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { useRouter } from "next/navigation"
import { toast } from "sonner"

import { Button } from "@/components/ui/button"
import { Form } from "@/components/ui/form"
import { isValidCpf } from "@/lib/cpf"
import { BrazilianState, LgpdPolicyConfig } from "@/lib/constants"
import { generateSHA256 } from "@/lib/hash"

import { StepPersonalData } from "./StepPersonalData"
import { StepAddress } from "./StepAddress"
import { StepConsent } from "./StepConsent"

const formSchema = z.object({
  // Step 1
  nome: z.string().min(3, "Nome completo é obrigatório"),
  cpf: z.string().refine((val) => isValidCpf(val), { message: "CPF inválido" }),
  rg: z.string().optional(),
  email: z.string().email("E-mail inválido"),
  telefone: z.string().min(10, "Telefone inválido"),
  dataNascimento: z.string().refine((val) => {
    const date = new Date(val)
    if (isNaN(date.getTime())) return false
    const age = (new Date().getTime() - date.getTime()) / (1000 * 60 * 60 * 24 * 365.25)
    return age >= 16 && age <= 100
  }, { message: "A idade deve ser entre 16 e 100 anos" }),

  // Step 2
  cep: z.string().min(8, "CEP inválido"),
  rua: z.string().min(2, "Rua é obrigatória"),
  numero: z.string().min(1, "Número é obrigatório"),
  complemento: z.string().optional(),
  bairro: z.string().min(2, "Bairro é obrigatório"),
  cidade: z.string().min(2, "Cidade é obrigatória"),
  estado: z.enum(BrazilianState, { errorMap: () => ({ message: "Estado inválido" }) }),

  // Step 3
  consentimento: z.boolean().refine((val) => val === true, {
    message: "Você deve aceitar a Política de Privacidade para continuar",
  }),
})

type FormValues = z.infer<typeof formSchema>

const steps = [
  { id: "personal", title: "Dados Pessoais", fields: ["nome", "cpf", "rg", "email", "telefone", "dataNascimento"] },
  { id: "address", title: "Endereço", fields: ["cep", "rua", "numero", "complemento", "bairro", "cidade", "estado"] },
  { id: "consent", title: "Consentimento", fields: ["consentimento"] },
]

export function RegistrationStepper() {
  const router = useRouter()
  const [currentStep, setCurrentStep] = React.useState(0)
  const [isSubmitting, setIsSubmitting] = React.useState(false)

  const form = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      nome: "",
      cpf: "",
      rg: "",
      email: "",
      telefone: "",
      dataNascimento: "",
      cep: "",
      rua: "",
      numero: "",
      complemento: "",
      bairro: "",
      cidade: "",
      estado: undefined,
      consentimento: false,
    },
    mode: "onChange",
  })

  const nextStep = async () => {
    const fields = steps[currentStep].fields as any
    const isValid = await form.trigger(fields, { shouldFocus: true })
    if (isValid) {
      setCurrentStep((prev) => Math.min(prev + 1, steps.length - 1))
    }
  }

  const prevStep = () => {
    setCurrentStep((prev) => Math.max(prev - 1, 0))
  }

  const onSubmit = async (values: FormValues) => {
    try {
      setIsSubmitting(true)
      
      const policyContentHash = await generateSHA256(LgpdPolicyConfig.text)
      const payload = {
        ...values,
        policyVersion: LgpdPolicyConfig.version,
        policyContentHash,
      }
      console.log("Submitting payload:", payload)

      // Simulate API call
      await new Promise((resolve) => setTimeout(resolve, 1000))
      
      // Simulating a 409 Conflict if CPF is a specific mocked one (for testing)
      if (values.cpf.replace(/\\D/g, "") === "00000000000") {
         throw new Error("409_CONFLICT")
      }

      toast.success("Aluno cadastrado com sucesso.")
      // Fake ID for redirect
      const studentId = "student-123"
      router.push(`/alunos/${studentId}`)
    } catch (error: any) {
      if (error.message === "409_CONFLICT") {
        setCurrentStep(0)
        form.setError("cpf", { type: "manual", message: "CPF já cadastrado no sistema." })
        toast.error("Erro: CPF já cadastrado no sistema.")
      } else {
        toast.error("Ocorreu um erro ao cadastrar o aluno.")
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="mx-auto max-w-2xl p-6 bg-card rounded-lg shadow-sm border">
      <div className="mb-8">
        <h2 className="text-xl font-semibold mb-2">Cadastrar Novo Aluno</h2>
        <div className="flex justify-between items-center text-sm text-muted-foreground">
          {steps.map((step, index) => (
            <div key={step.id} className="flex items-center">
              <div
                className={`flex items-center justify-center w-8 h-8 rounded-full border-2 ${
                  index <= currentStep ? "border-primary text-primary" : "border-muted"
                }`}
              >
                {index + 1}
              </div>
              <span className={`ml-2 hidden sm:block ${index <= currentStep ? "text-foreground" : ""}`}>
                {step.title}
              </span>
              {index < steps.length - 1 && (
                <div className={`w-12 h-[2px] mx-2 ${index < currentStep ? "bg-primary" : "bg-muted"}`} />
              )}
            </div>
          ))}
        </div>
      </div>

      <Form {...form}>
        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
          <div className={currentStep === 0 ? "block" : "hidden"}>
            <StepPersonalData />
          </div>
          <div className={currentStep === 1 ? "block" : "hidden"}>
            <StepAddress />
          </div>
          <div className={currentStep === 2 ? "block" : "hidden"}>
            <StepConsent />
          </div>

          <div className="flex justify-between pt-6 border-t mt-8">
            <Button
              type="button"
              variant="outline"
              onClick={prevStep}
              disabled={currentStep === 0 || isSubmitting}
            >
              Voltar
            </Button>
            {currentStep < steps.length - 1 ? (
              <Button type="button" onClick={nextStep}>
                Próximo
              </Button>
            ) : (
              <Button
                type="submit"
                disabled={!form.watch("consentimento") || isSubmitting}
              >
                {isSubmitting ? "Cadastrando..." : "Finalizar Cadastro"}
              </Button>
            )}
          </div>
        </form>
      </Form>
    </div>
  )
}
