"use client"

import * as React from "react"
import { useFormContext } from "react-hook-form"

import {
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { BrazilianState } from "@/lib/constants"

export function StepAddress() {
  const { control, setValue } = useFormContext()

  const handleCepChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    let value = e.target.value.replace(/\D/g, "")
    if (value.length > 5) {
      value = value.replace(/^(\d{5})(\d)/, "$1-$2")
    }
    setValue("cep", value.slice(0, 9), { shouldValidate: true })

    // Auto-fill via ViaCEP if 8 digits
    const pureCep = value.replace(/\D/g, "")
    if (pureCep.length === 8) {
      try {
        const response = await fetch(`https://viacep.com.br/ws/${pureCep}/json/`)
        const data = await response.json()
        if (!data.erro) {
          setValue("rua", data.logradouro, { shouldValidate: true })
          setValue("bairro", data.bairro, { shouldValidate: true })
          setValue("cidade", data.localidade, { shouldValidate: true })
          if (BrazilianState.includes(data.uf as any)) {
            setValue("estado", data.uf, { shouldValidate: true })
          }
        }
      } catch (error) {
        // Ignore errors silently for autofill
        console.error("ViaCEP error", error)
      }
    }
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
      <FormField
        control={control}
        name="cep"
        render={({ field }) => (
          <FormItem>
            <FormLabel>CEP</FormLabel>
            <FormControl>
              <Input 
                placeholder="00000-000" 
                {...field} 
                onChange={handleCepChange} 
                maxLength={9} 
              />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />

      <div className="hidden md:block" />

      <FormField
        control={control}
        name="rua"
        render={({ field }) => (
          <FormItem className="md:col-span-2">
            <FormLabel>Rua / Logradouro</FormLabel>
            <FormControl>
              <Input placeholder="Rua das Flores" {...field} />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />

      <FormField
        control={control}
        name="numero"
        render={({ field }) => (
          <FormItem>
            <FormLabel>Número</FormLabel>
            <FormControl>
              <Input placeholder="123" {...field} />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />

      <FormField
        control={control}
        name="complemento"
        render={({ field }) => (
          <FormItem>
            <FormLabel>Complemento (Opcional)</FormLabel>
            <FormControl>
              <Input placeholder="Apto 45" {...field} />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />

      <FormField
        control={control}
        name="bairro"
        render={({ field }) => (
          <FormItem>
            <FormLabel>Bairro</FormLabel>
            <FormControl>
              <Input placeholder="Centro" {...field} />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />

      <FormField
        control={control}
        name="cidade"
        render={({ field }) => (
          <FormItem>
            <FormLabel>Cidade</FormLabel>
            <FormControl>
              <Input placeholder="São Paulo" {...field} />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />

      <FormField
        control={control}
        name="estado"
        render={({ field }) => (
          <FormItem>
            <FormLabel>Estado</FormLabel>
            <Select onValueChange={field.onChange} value={field.value || ""}>
              <FormControl>
                <SelectTrigger>
                  <SelectValue placeholder="Selecione..." />
                </SelectTrigger>
              </FormControl>
              <SelectContent>
                {BrazilianState.map((uf) => (
                  <SelectItem key={uf} value={uf}>
                    {uf}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <FormMessage />
          </FormItem>
        )}
      />
    </div>
  )
}
