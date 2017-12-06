<template>
  <v-container fluid class='layer-list'>
    <br>
    <layer v-for='layer in layers' :key='layer.guid' :layer='layer' :clientId='clientId' class='layer'></layer>
  </v-container>
</template>

<script>
  import Layer from './ReceiverLayersLayer.vue'

  export default {
    name: 'ReceiverLayers',
    props: {
      layers: Array,
      clientId: String
    },
    components: {
      Layer
    },
    computed: {},
    data() {
      return {
      }
    },
    methods: {
    },
    mounted() {
    }
  }
</script>

<style lang='scss'>
  .layer-list{
    padding: 0;
  }

  .layer {
    padding: 10px 0 10px 0;
    background-color: rgba(25,25,25,0);
    transition: all .3s ease;
  }

  .layer:hover {
    background-color: #CCCCCC;
  }
</style>