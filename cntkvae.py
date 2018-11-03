import sys
import os
import cntk as C

import matplotlib.pyplot as plt
import matplotlib.image as mpimg

from cntk.train import Trainer, minibatch_size_schedule 
from cntk.io import MinibatchSource, CTFDeserializer, StreamDef, StreamDefs, INFINITELY_REPEAT
from cntk.device import cpu, try_set_default_device
from cntk.learners import adadelta, learning_parameter_schedule_per_sample
from cntk.ops import relu, element_times, constant
from cntk.layers import Dense, Sequential, For
from cntk.losses import cross_entropy_with_softmax
from cntk.metrics import classification_error
from cntk.train.training_session import *
from cntk.logging import ProgressPrinter, TensorBoardProgressWriter
import cntk.tests.test_utils

import datetime

C.tests.test_utils.set_device_from_pytest_env() # (only needed for our build system)
C.cntk_py.set_fixed_random_seed(1) # fix a random seed for CNTK components
dir_path = os.path.dirname(os.path.realpath(__file__))

isFast = True
num_epochs = 128 * 100

width_dim = 7
height_dim = 129
input_dim = width_dim * height_dim
latent_dim = 8

output_dim = input_dim

model_filename = os.path.join(dir_path,'binancevae.dnn')
train_filename = os.path.join(dir_path,'pictureset_training.ctf')
model_graph = os.path.join(dir_path,'binancevae.png')

def displaynetwork(model):
    C.logging.graph.plot(model, model_graph)

def create_reader(path, is_training, input_dim, num_label_classes,max_sweeps=C.INFINITELY_REPEAT):
    return C.io.MinibatchSource(C.io.CTFDeserializer(path, C.io.StreamDefs(labels = C.io.StreamDef(field='L', shape=num_label_classes, is_sparse=False),
        features   = C.io.StreamDef(field='F', shape=input_dim, is_sparse=False))), randomize = is_training, max_sweeps =max_sweeps)


def vae_loss(model_output, label,z_mean,z_log_var):
    # compute the average MSE error, then scale it up, ie.  simply sum on all
    # axes
    
    reconstruction_loss = C.reduce_sum(C.squared_error(model_output,label))
    # compute the KL loss
    kl_loss = - 0.5 * C.reduce_sum(1 + z_log_var - C.square(z_mean) - C.square(C.exp(z_log_var)))
    # return the average loss over all images in batch
    total_loss = C.reduce_mean(reconstruction_loss + kl_loss)    
    return total_loss

def sampling(z_mean,z_log_var):
    """Reparameterization trick by sampling fr an isotropic unit Gaussian.
    # Arguments:
        args (tensor): mean and log of variance of Q(z|X)
    # Returns:
        z (tensor): sampled latent vector
    """
    # by default, random_normal has mean=0 and std=1.0
    epsilon = C.random.normal(shape=z_mean.shape)
    return z_mean + C.exp(z_log_var) * epsilon

def createmodel(features,latent_dim):
    model = C.layers.Label('input_encoder')(features)
    
    model = C.layers.Dense(128)(model)
    #model = C.layers.BatchNormalization()(model)
    model = C.layers.Activation(activation=C.relu)(model)
    
    model = C.layers.Dense(16)(model)
    #model = C.layers.BatchNormalization()(model)
    model = C.layers.Activation(activation=C.relu)(model)
    
    model = C.layers.Label('output_driver_latent_layer')(model)
    model = C.layers.Label('input_driver_latent_layer')(model)

    z_mean = C.layers.Dense(latent_dim)(model)
    z_mean = C.layers.BatchNormalization()(z_mean)
    z_mean = C.layers.Label('z_mean')(z_mean)

    z_log_var = C.layers.Dense(latent_dim)(model)
    z_log_var = C.layers.Activation(activation=C.relu)(z_log_var)
    z_log_var = C.layers.BatchNormalization()(z_log_var)
    z_log_var = C.layers.Label('z_log_var')(z_log_var)    

    model = sampling(z_mean,z_log_var)
    #model = C.plus(z_mean, C.exp(z_log_var) * C.random.normal(shape=latent_dim))
    
    model = C.layers.Label('output_encoder')(model)
    model = C.layers.Label('input_decoder')(model)

    model = C.layers.Dense(16)(model)
    #model = C.layers.BatchNormalization()(model)
    model = C.layers.Activation(activation=C.relu)(model)

    model = C.layers.Dense(128)(model)
    #model = C.layers.BatchNormalization()(model)
    model = C.layers.Activation(activation=C.relu)(model)

    model = C.layers.Dense(features.shape,activation=C.relu)(model)
    model = C.clip(model,0.,1.)
    model = C.layers.Label('output_decoder')(model)
    return model,z_mean,z_log_var

def train(reader_train,model,input,z_mean,z_log_var):

    label = C.input_variable(input.shape)
    target = label

    loss = vae_loss(model,target,z_mean,z_log_var)
    #loss = C.squared_error(model, target)
    label_error = C.squared_error(model, target)

    # training config
    
    minibatch_size = 4
    epoch_size = 40000 // minibatch_size        # 30000 samples is half the dataset size
    num_sweeps_to_train_with = 5 if isFast else 100
    num_samples_per_sweep = 60000
    num_minibatches_to_train = (num_samples_per_sweep * num_sweeps_to_train_with) // minibatch_size


    # Instantiate the trainer object to drive the model training
    lr_per_sample = [0.001]
    lr_schedule = C.learning_parameter_schedule_per_sample(lr_per_sample, epoch_size)

    # Momentum which is applied on every minibatch_size = 64 samples
    momentum_schedule = C.momentum_schedule(0.9126265014311797, minibatch_size)

    # We use a variant of the Adam optimizer which is known to work well on
    # this dataset
    # Feel free to try other optimizers from
    # https://www.cntk.ai/pythondocs/cntk.learner.html#module-cntk.learner
    
    learner = C.fsadagrad(model.parameters,lr=lr_schedule, momentum=momentum_schedule)

    #lr_schedule = C.learning_rate_schedule( 0.1, C.UnitType.minibatch)
    #learner = C.sgd(model.parameters, lr_schedule)

    # Instantiate the trainer
    progress_printer = C.logging.ProgressPrinter(128)
    trainer = C.Trainer(model, (loss, label_error), learner, progress_printer)

    # Map the data streams to the input and labels.
    # Note: for autoencoders input == label
    input_map = {
        input  : reader_train.streams.features,
        label  : reader_train.streams.labels
    }

    aggregate_metric = 0
    
    for i in range(num_epochs):
    #for i in range(16):
        # Read a mini batch from the training data file
        total_epoch_samples = 0
        print('epoch #',str(i))
        for _ in range(epoch_size):
            data = reader_train.next_minibatch(minibatch_size, input_map = input_map)
            #if len(data)==0:
            #    break
        # Run the trainer on and perform model training
            trainer.train_minibatch(data)
            samples = trainer.previous_minibatch_sample_count
            total_epoch_samples +=  samples
            aggregate_metric += trainer.previous_minibatch_evaluation_average * samples
        model.save(model_filename)
    train_error = (aggregate_metric * 100.0) / (trainer.total_number_of_samples_seen)
    print("Average training error: {0:0.2f}%".format(train_error))
    return train_error


create_model = False

if not os.path.exists(model_filename):
    create_model = True

if create_model:
    features = C.input_variable(shape=input_dim)
    print('create model')
    model,z_mean,z_log_var = createmodel(features,latent_dim)
    displaynetwork(model)

else:
    model = C.load_model(model_filename)
    features = model.arguments[0]
    z_mean = model.find_by_name('z_mean')
    z_log_var = model.find_by_name('z_log_var')


reader_train = create_reader(train_filename, True, input_dim, output_dim)

vae_train_error = train(reader_train,model,features,z_mean,z_log_var)
