import { Metadata } from "next"
import { RegistrationStepper } from "./RegistrationStepper"

export const metadata: Metadata = {
  title: "Novo Aluno - CFCHub",
  description: "Cadastrar um novo aluno no sistema",
}

export default function NovoAlunoPage() {
  return (
    <div className="container mx-auto py-10">
      <RegistrationStepper />
    </div>
  )
}
