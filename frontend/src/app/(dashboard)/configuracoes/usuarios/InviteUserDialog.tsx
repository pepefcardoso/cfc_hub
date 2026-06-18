'use client';

import { useState } from 'react';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { toast } from 'sonner';

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { inviteStaffUser } from '@/hooks/useStaffUsers';
import { ApiError } from '@/lib/api/errors';

const inviteSchema = z.object({
  name: z.string().min(2, 'O nome deve ter pelo menos 2 caracteres.'),
  email: z.string().email('E-mail inválido.'),
  role: z.string().min(1, 'Selecione um papel.'),
  password: z
    .string()
    .min(12, 'A senha deve ter pelo menos 12 caracteres.')
    .regex(/[A-Z]/, 'A senha deve conter pelo menos uma letra maiúscula.')
    .regex(/[0-9]/, 'A senha deve conter pelo menos um número.')
    .regex(/[^A-Za-z0-9]/, 'A senha deve conter pelo menos um caractere especial.'),
});

type InviteFormData = z.infer<typeof inviteSchema>;

interface InviteUserDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSuccess: () => void;
}

export function InviteUserDialog({ open, onOpenChange, onSuccess }: InviteUserDialogProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);

  const {
    register,
    handleSubmit,
    control,
    setError,
    reset,
    watch,
    formState: { errors },
  } = useForm<InviteFormData>({
    resolver: zodResolver(inviteSchema),
    defaultValues: {
      name: '',
      email: '',
      role: '',
      password: '',
    },
  });

  const onSubmit = async (data: InviteFormData) => {
    try {
      setIsSubmitting(true);
      await inviteStaffUser(data);
      toast.success('Usuário convidado com sucesso.');
      reset();
      onSuccess();
      onOpenChange(false);
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        // Assuming the backend maps EMAIL_IN_USE or similar conflict to 409
        setError('email', { type: 'manual', message: 'Este e-mail já está em uso.' });
      } else {
        toast.error('Ocorreu um erro ao convidar o usuário.');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleOpenChange = (newOpen: boolean) => {
    if (!newOpen) {
      reset();
    }
    onOpenChange(newOpen);
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-[425px]">
        <form onSubmit={handleSubmit(onSubmit)}>
          <DialogHeader>
            <DialogTitle>Convidar Usuário</DialogTitle>
            <DialogDescription>
              Preencha os dados abaixo para convidar um novo membro da equipe.
            </DialogDescription>
          </DialogHeader>

          <div className="grid gap-4 py-4">
            <div className="grid gap-2">
              <Label htmlFor="name">Nome</Label>
              <Input
                id="name"
                placeholder="Ex: João Silva"
                {...register('name')}
                disabled={isSubmitting}
                aria-invalid={!!errors.name}
              />
              {errors.name && (
                <p className="text-sm font-medium text-destructive">{errors.name.message}</p>
              )}
            </div>

            <div className="grid gap-2">
              <Label htmlFor="email">E-mail</Label>
              <Input
                id="email"
                type="email"
                placeholder="Ex: joao@autoescola.com"
                {...register('email')}
                disabled={isSubmitting}
                aria-invalid={!!errors.email}
              />
              {errors.email && (
                <p className="text-sm font-medium text-destructive">{errors.email.message}</p>
              )}
            </div>

            <div className="grid gap-2">
              <Label htmlFor="role">Papel</Label>
              <Controller
                control={control}
                name="role"
                render={({ field }) => (
                  <Select
                    onValueChange={field.onChange}
                    defaultValue={field.value}
                    disabled={isSubmitting}
                  >
                    <SelectTrigger aria-invalid={!!errors.role}>
                      <SelectValue placeholder="Selecione um papel" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="Admin">Administrador (Admin)</SelectItem>
                      <SelectItem value="Receptionist">Recepcionista</SelectItem>
                      <SelectItem value="Instructor">Instrutor</SelectItem>
                      <SelectItem value="Financial">Financeiro</SelectItem>
                    </SelectContent>
                  </Select>
                )}
              />
              {errors.role && (
                <p className="text-sm font-medium text-destructive">{errors.role.message}</p>
              )}
            </div>

            <div className="grid gap-2">
              <Label htmlFor="password">Senha Temporária</Label>
              <Input
                id="password"
                type="password"
                {...register('password')}
                disabled={isSubmitting}
                aria-invalid={!!errors.password}
              />
              
              {/* Password strength feedback inline */}
              {(() => {
                const pwd = watch('password') || '';
                const hasMinLength = pwd.length >= 12;
                const hasUppercase = /[A-Z]/.test(pwd);
                const hasNumber = /[0-9]/.test(pwd);
                const hasSpecialChar = /[^A-Za-z0-9]/.test(pwd);
                const criteriaMetCount = [hasMinLength, hasUppercase, hasNumber, hasSpecialChar].filter(Boolean).length;
                
                const strengthColor = criteriaMetCount === 4 ? 'bg-green-500' 
                  : criteriaMetCount >= 2 ? 'bg-yellow-500' 
                  : criteriaMetCount > 0 ? 'bg-red-500' : 'bg-muted';
                
                return (
                  <div className="mt-1 space-y-2">
                    <div className="flex h-1.5 w-full overflow-hidden rounded-full bg-cfc-surface-200">
                      <div className={`h-full ${strengthColor} transition-all`} style={{ width: `${(criteriaMetCount / 4) * 100}%` }} />
                    </div>
                    <ul className="text-xs space-y-1 text-muted-foreground">
                      <li className={hasMinLength ? 'text-green-600 dark:text-green-500' : ''}>
                        {hasMinLength ? '✓' : '○'} Pelo menos 12 caracteres
                      </li>
                      <li className={hasUppercase ? 'text-green-600 dark:text-green-500' : ''}>
                        {hasUppercase ? '✓' : '○'} Letra maiúscula
                      </li>
                      <li className={hasNumber ? 'text-green-600 dark:text-green-500' : ''}>
                        {hasNumber ? '✓' : '○'} Número
                      </li>
                      <li className={hasSpecialChar ? 'text-green-600 dark:text-green-500' : ''}>
                        {hasSpecialChar ? '✓' : '○'} Caractere especial
                      </li>
                    </ul>
                  </div>
                );
              })()}

              {errors.password && (
                <p className="text-sm font-medium text-destructive mt-1">{errors.password.message}</p>
              )}
            </div>
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => handleOpenChange(false)}
              disabled={isSubmitting}
            >
              Cancelar
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Convidando...' : 'Convidar'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
