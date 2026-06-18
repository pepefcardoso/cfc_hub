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
import { formatCpf } from "@/lib/cpf"

export function StepPersonalData() {
  const { control, setValue } = useFormContext()

  const handleCpfChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const formatted = formatCpf(e.target.value)
    setValue("cpf", formatted, { shouldValidate: true })
  }

  const handlePhoneChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    // Basic mask for phone: +55 (XX) XXXXX-XXXX
    let value = e.target.value.replace(/\D/g, "")
    if (value.startsWith("55")) value = value.slice(2)
    
    let formatted = "+55 "
    if (value.length > 0) {
      formatted += `(${value.slice(0, 2)}`
    }
    if (value.length > 2) {
      formatted += `) ${value.slice(2, 7)}`
    }
    if (value.length > 7) {
      formatted += `-${value.slice(7, 11)}`
    }
    
    setValue("telefone", formatted.trim(), { shouldValidate: true })
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
      <FormField
        control={control}
        name="nome"
        render={({ field }) => (
          <FormItem className="md:col-span-2">
            <FormLabel>Nome Completo</FormLabel>
            <FormControl>
              <Input placeholder="João da Silva" {...field} />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />

      <FormField
        control={control}
        name="cpf"
        render={({ field }) => (
          <FormItem>
            <FormLabel>CPF</FormLabel>
            <FormControl>
              <Input 
                placeholder="000.000.000-00" 
                {...field} 
                onChange={handleCpfChange} 
                maxLength={14} 
              />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />

      <FormField
        control={control}
        name="rg"
        render={({ field }) => (
          <FormItem>
            <FormLabel>RG (Opcional)</FormLabel>
            <FormControl>
              <Input placeholder="00.000.000-X" {...field} />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />

      <FormField
        control={control}
        name="email"
        render={({ field }) => (
          <FormItem>
            <FormLabel>E-mail</FormLabel>
            <FormControl>
              <Input type="email" placeholder="joao@exemplo.com" {...field} />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />

      <FormField
        control={control}
        name="telefone"
        render={({ field }) => (
          <FormItem>
            <FormLabel>Telefone</FormLabel>
            <FormControl>
              <Input 
                placeholder="+55 (11) 99999-9999" 
                {...field} 
                onChange={handlePhoneChange} 
                maxLength={19} 
              />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />

      <FormField
        control={control}
        name="dataNascimento"
        render={({ field }) => (
          <FormItem>
            <FormLabel>Data de Nascimento</FormLabel>
            <FormControl>
              <Input type="date" {...field} />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />
    </div>
  )
}
